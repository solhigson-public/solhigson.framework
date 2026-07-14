using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Solhigson.Framework.Data.Attributes;
using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// Fail-CLOSED same-database audit-capture interceptor (F2). At <c>SavingChanges[Async]</c> it enumerates
/// the change tracker, materializes an <see cref="AuditTrail"/> row per capture-eligible entry, and
/// <c>Add()</c>s those rows into the SAME <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker"/>
/// so they ride the business write's own command batch and implicit transaction — atomic with the write, no
/// broker, no owned transaction. Any throw during materialization aborts the whole <c>SaveChanges</c>
/// (structural fail-closed): there is deliberately NO broad catch anywhere on this path. It is the exact
/// inverse of the fail-OPEN <c>ListingProjectionInterceptor</c>, whose swallowing catches MUST NOT be copied.
///
/// <para><b>Activation gate (pin R4).</b> Materialization is a natural no-op unless the context has mapped
/// <see cref="AuditTrail"/> (<c>context.Model.FindEntityType(typeof(AuditTrail)) is not null</c>): the
/// framework's own fixed-model contexts and any pre-migration consumer skip capture entirely. Fail-closed
/// applies only WITHIN a mapped, interceptor-wired context.</para>
///
/// <para><b>No per-save state.</b> Capture is single-phase (snapshot → materialize → <c>Add()</c>, all before
/// the save runs), so unlike <c>ListingProjectionInterceptor</c> this interceptor needs NO
/// <c>ConditionalWeakTable</c> and NO instance fields for per-save state — the anti-pattern of the caching
/// interceptor's instance-field list is avoided by construction. The injected seams
/// (<see cref="IAuditActorProvider"/>, <see cref="AuditCaptureRegistry"/>, <see cref="AuditFieldMasker"/>) are
/// singleton-safe and read-only per save, so a single instance safely serves pooled contexts across scopes.</para>
///
/// <para><b>Recursion exclusion (pin R2.1).</b> <see cref="AuditTrail"/> is hard-excluded from eligibility, and
/// the eligible entries are snapshotted to a list BEFORE any <c>Add()</c>, so the rows this save inserts are
/// never themselves audited.</para>
/// </summary>
public sealed class AuditCaptureSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private readonly IAuditActorProvider _actorProvider;
    private readonly AuditCaptureRegistry _registry;
    private readonly AuditFieldMasker _masker;

    /// <summary>
    /// Resolves the actor seam, the fluent registry, and the masking options. <paramref name="actorProvider"/>
    /// MUST be a singleton-safe implementation (pin R5): the interceptor is captured across scopes by pooled
    /// contexts, so a captured scoped provider would leak. The framework default
    /// (<see cref="UnattributedAuditActorProvider"/>) is registered <c>SingleInstance</c>; the web/Hangfire
    /// AsyncLocal-backed providers land at E3.
    /// </summary>
    public AuditCaptureSaveChangesInterceptor(
        IAuditActorProvider actorProvider,
        AuditCaptureRegistry registry,
        AuditCaptureOptions options)
    {
        _actorProvider = actorProvider ?? throw new ArgumentNullException(nameof(actorProvider));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _masker = new AuditFieldMasker(options);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        // Capture is fully synchronous (change-tracker reads + Add(), no I/O), so the sync and async
        // entry points share ONE implementation — no duplicated logic to drift.
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken: cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        // Activation gate (pin R4): no-op on any context that has not mapped AuditTrail.
        if (context is null || context.Model.FindEntityType(typeof(AuditTrail)) is null)
        {
            return;
        }

        // Snapshot the eligible entries BEFORE Add()-ing any audit row: the rows we insert must not be
        // enumerated (recursion exclusion), and the tracker must not be mutated mid-enumeration.
        List<AuditTrail>? auditRows = null;
        var actor = _actorProvider.GetCurrentActor();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (!IsCaptureEligible(entry.Metadata.ClrType))
            {
                continue;
            }

            var row = MaterializeAuditRow(entry, actor);
            if (row is not null)
            {
                (auditRows ??= []).Add(row);
            }
        }

        if (auditRows is null)
        {
            return;
        }

        foreach (var row in auditRows)
        {
            context.Add(row);
        }
    }

    /// <summary>
    /// The pinned eligibility predicate (R2): (1) hard-exclude <see cref="AuditTrail"/> (recursion); (2) an
    /// ignore in EITHER source wins — class <c>[SolhigsonAuditIgnore]</c> (inherit:false) OR registry-ignored;
    /// (3) captured iff class <c>[SolhigsonAuditInclude]</c> (inherit:false) OR <see cref="AuditCaptureRegistry.IsIncluded"/>.
    /// </summary>
    private bool IsCaptureEligible(Type type)
    {
        if (type == typeof(AuditTrail))
        {
            return false;
        }

        if (type.IsDefined(typeof(SolhigsonAuditIgnoreAttribute), inherit: false) || _registry.IsIgnored(type))
        {
            return false;
        }

        return type.IsDefined(typeof(SolhigsonAuditIncludeAttribute), inherit: false) || _registry.IsIncluded(type);
    }

    private AuditTrail? MaterializeAuditRow(EntityEntry entry, AuditActor actor)
    {
        var row = new AuditTrail
        {
            Category = AuditEventCategory.DataChange,
            EntityType = entry.Metadata.ClrType.Name,
            EntityId = BuildEntityId(entry),
            ActorUserId = actor.ActorUserId,
            UserDisplayName = actor.UserDisplayName,
            UserIp = actor.UserIp,
            SourceType = actor.SourceType,
            SourceId = actor.SourceId,
        };

        switch (entry.State)
        {
            case EntityState.Added:
                row.Snapshot = BuildSnapshotJson(entry, useOriginalValues: false);
                break;
            case EntityState.Deleted:
                row.Snapshot = BuildSnapshotJson(entry, useOriginalValues: true);
                break;
            case EntityState.Modified:
                var changes = BuildChangesJson(entry);
                if (changes is null)
                {
                    // A "Modified" entry with no captured scalar delta (only ignored/unmodified columns
                    // changed) yields no audit row.
                    return null;
                }

                row.Changes = changes;
                break;
            default:
                return null;
        }

        return row;
    }

    private static string BuildEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return string.Empty;
        }

        var useOriginal = entry.State == EntityState.Deleted;
        var parts = new List<string>(key.Properties.Count);
        foreach (var keyProperty in key.Properties)
        {
            var propertyEntry = entry.Property(keyProperty.Name);
            var value = useOriginal ? propertyEntry.OriginalValue : propertyEntry.CurrentValue;
            parts.Add(value?.ToString() ?? string.Empty);
        }

        return string.Join(",", parts);
    }

    private string BuildSnapshotJson(EntityEntry entry, bool useOriginalValues)
    {
        var snapshot = new Dictionary<string, object?>();
        foreach (var propertyEntry in entry.Properties)
        {
            if (IsPropertyIgnored(propertyEntry.Metadata.PropertyInfo))
            {
                continue;
            }

            var rawValue = useOriginalValues ? propertyEntry.OriginalValue : propertyEntry.CurrentValue;
            snapshot[propertyEntry.Metadata.Name] = MaskIfSensitive(propertyEntry, rawValue);
        }

        return JsonSerializer.Serialize(snapshot, PayloadJsonOptions);
    }

    private string? BuildChangesJson(EntityEntry entry)
    {
        List<FieldChange>? changes = null;
        foreach (var propertyEntry in entry.Properties)
        {
            if (!propertyEntry.IsModified)
            {
                continue;
            }

            if (IsPropertyIgnored(propertyEntry.Metadata.PropertyInfo))
            {
                continue;
            }

            // Both sides masked (pin item 5): a new-side-only mask would leak the pre-change plaintext.
            var oldValue = MaskIfSensitive(propertyEntry, propertyEntry.OriginalValue);
            var newValue = MaskIfSensitive(propertyEntry, propertyEntry.CurrentValue);
            (changes ??= []).Add(new FieldChange(propertyEntry.Metadata.Name, oldValue, newValue));
        }

        return changes is null ? null : JsonSerializer.Serialize(changes, PayloadJsonOptions);
    }

    private object? MaskIfSensitive(PropertyEntry propertyEntry, object? rawValue)
        => _masker.ShouldMask(propertyEntry.Metadata.Name, propertyEntry.Metadata.PropertyInfo)
            ? AuditFieldMasker.MaskMarker
            : rawValue;

    private static bool IsPropertyIgnored(PropertyInfo? propertyInfo)
        => propertyInfo is not null
           && propertyInfo.IsDefined(typeof(SolhigsonAuditIgnoreAttribute), inherit: false);

    /// <summary>The pinned E7-consumed UPDATE-delta element shape: an array of these.</summary>
    private sealed record FieldChange(
        [property: JsonPropertyName("field")] string Field,
        [property: JsonPropertyName("old")] object? Old,
        [property: JsonPropertyName("new")] object? New);
}

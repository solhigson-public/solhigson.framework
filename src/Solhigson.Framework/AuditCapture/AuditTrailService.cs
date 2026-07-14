using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// Default <see cref="IAuditTrailService{TContext}"/> implementation (F3). Writes one
/// <see cref="AuditTrail"/> row per call via the bound consumer context: construct (Id/Created defaults
/// from the F1 entity initializers), <c>Add</c>, <c>SaveChangesAsync</c>. The row rides any ambient
/// transaction on <typeparamref name="TContext"/> automatically — no owned transaction, no broker.
///
/// <para><b>Activation gate.</b> Mirrors <see cref="AuditCaptureSaveChangesInterceptor"/>: when the bound
/// context has not mapped <see cref="AuditTrail"/> (<c>Model.FindEntityType(typeof(AuditTrail)) is null</c> —
/// the framework's own fixed-model contexts, or a pre-migration consumer), the call is a no-op. Unlike the
/// interceptor's silent miss, the no-op branch here logs a WARNING: a consumer that wired the service but
/// forgot the mapping would otherwise silently drop security events.</para>
///
/// <para><b>Fail-closed.</b> There is deliberately NO catch anywhere in <see cref="LogAsync"/>: a failed
/// save, a cancelled token, or a non-serializable descriptor throws to the caller. Coexistence with the F2
/// interceptors is by construction — the row is <see cref="Microsoft.EntityFrameworkCore.EntityState.Added"/>
/// (permitted by <see cref="AuditTrailAppendOnlyInterceptor"/>) and <see cref="AuditTrail"/> is hard-excluded
/// from capture eligibility (no audit-of-audit).</para>
/// </summary>
/// <typeparam name="TContext">The consumer <see cref="DbContext"/> whose model maps <see cref="AuditTrail"/>.</typeparam>
public sealed class AuditTrailService<TContext>(TContext context) : IAuditTrailService<TContext>
    where TContext : DbContext
{
    private static readonly LogWrapper Logger = LogManager.GetLogger(nameof(AuditTrailService<TContext>));

    /// <summary>Same serializer posture as the F2 capture interceptor's payloads.</summary>
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private readonly TContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public async Task LogAsync(
        AuditEventCategory category,
        string entityType,
        string entityId,
        AuditActor actor,
        object payloadOrDescriptor,
        CancellationToken cancellationToken)
    {
        if (category is not (AuditEventCategory.SecurityEvent or AuditEventCategory.BusinessEvent))
        {
            throw new ArgumentOutOfRangeException(nameof(category), category,
                "Explicit audit logging accepts SecurityEvent or BusinessEvent only. DataChange is owned " +
                "by AuditCaptureSaveChangesInterceptor (and is the enum default — an omitted category is a " +
                "caller bug, not a data change); undefined values would corrupt the persisted wire contract.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(payloadOrDescriptor);

        // Activation gate — mirror of AuditCaptureSaveChangesInterceptor.Capture, plus the R5 signal:
        // a wired-but-unmapped consumer must not silently drop security events.
        if (_context.Model.FindEntityType(typeof(AuditTrail)) is null)
        {
            Logger.LogWarning(
                "Explicit audit event DROPPED: context {ContextType} has not mapped AuditTrail in its model, " +
                "so IAuditTrailService is a no-op on it. Map AuditTrail in OnModelCreating (and migrate) or " +
                "bind the service to the context that owns the audit table. " +
                "Dropped event: Category={Category}, EntityType={EntityType}, EntityId={EntityId}.",
                typeof(TContext).FullName, category, entityType, entityId);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var row = new AuditTrail
        {
            Category = category,
            EntityType = entityType,
            EntityId = entityId,
            Snapshot = JsonSerializer.Serialize(payloadOrDescriptor, PayloadJsonOptions),
            // Changes stays null: explicit events carry a descriptor snapshot, never a field delta.
            ActorUserId = actor.ActorUserId,
            UserDisplayName = actor.UserDisplayName,
            UserIp = actor.UserIp,
            SourceType = actor.SourceType,
            SourceId = actor.SourceId,
        };

        _context.Add(row);
        await _context.SaveChangesAsync(cancellationToken: cancellationToken);
    }
}

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// Default <see cref="IAuditTrailService{TContext}"/> implementation (F3-prime, §2 NEVER-BLOCK INVARIANT).
/// Builds one <see cref="AuditTrail"/> row per call (Id/Created defaults from the F1 entity initializers) and
/// hands it to the out-of-band <see cref="IAuditSink"/> for persistence — NOT an inline
/// <c>Add</c>+<c>SaveChangesAsync</c>+rethrow on the bound context. No caller ever observes an
/// audit-infrastructure exception.
///
/// <para><b>Never-block (M2).</b> The payload build (serialization) AND the sink handoff run inside one
/// swallow-all boundary: a serialization failure, an inline-write failure, or an enqueue failure is logged
/// (plus an <c>audit_capture_failed</c> metric) and swallowed — NEVER rethrown. The ONLY exceptions that
/// still surface are (1) the argument-contract guards below (caller bugs, deterministic, pre-boundary — not
/// audit-infrastructure failures) and (2) an <see cref="OperationCanceledException"/> from the caller's own
/// token (cooperative cancellation — the caller is abandoning; a consumer best-effort wrap rethrows it too).
/// There is NO outermost-commit tie here: unlike the F2-prime interceptor, <see cref="LogAsync"/> hands off
/// synchronously in the caller's scope; post-commit ordering for a wrapped caller is a CALLER contract, not
/// enforced here.</para>
///
/// <para><b>Transitional persisting-safe fallback.</b> When no consumer <see cref="IAuditSink"/> is registered
/// yet (the window before the consumer sink lands, while the shipped explicit-audit call sites already consume
/// this service), the row is persisted INLINE on the bound context via a tracked <c>Add</c>+<c>SaveChangesAsync</c>
/// — the shipped 10.4.0 behaviour MINUS the rethrow (failures swallowed). This keeps row-asserting consumer
/// integration gates green through the bump without ever blocking the caller; it is replaced by the out-of-band
/// sink once wired.</para>
///
/// <para><b>Activation gate (fallback path only).</b> On the transitional fallback, when the bound context has
/// not mapped <see cref="AuditTrail"/>, the call logs a WARNING and no-ops. When a sink is wired the sink owns
/// its own mapped write context, so this gate does not apply. <see cref="AuditTrail"/> stays hard-excluded from
/// F2-prime capture eligibility (no audit-of-audit).</para>
/// </summary>
/// <typeparam name="TContext">The consumer <see cref="DbContext"/> whose model maps <see cref="AuditTrail"/>.</typeparam>
public sealed class AuditTrailService<TContext>(TContext context, IAuditSink? sink = null) : IAuditTrailService<TContext>
    where TContext : DbContext
{
    private static readonly LogWrapper Logger = LogManager.GetLogger(nameof(AuditTrailService<TContext>));

    /// <summary>Same serializer posture as the F2 capture interceptor's payloads.</summary>
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private readonly TContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly IAuditSink? _sink = sink;

    /// <inheritdoc />
    public async Task LogAsync(
        AuditEventCategory category,
        string entityType,
        string entityId,
        AuditActor actor,
        object payloadOrDescriptor,
        CancellationToken cancellationToken,
        string? action = null,
        string? subjectDisplayName = null)
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

        // Cooperative cancellation: observe the caller's token before any work; a cancelled caller is
        // abandoning, so OperationCanceledException propagates (it is not an audit-infrastructure failure).
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var row = new AuditTrail
            {
                Category = category,
                // Explicit-path pass-through: stamped VERBATIM (an eventType-as-action is never forced
                // lowercase) within the declared column widths; an omitted param leaves the column null.
                Action = AuditTrail.Truncate(action, AuditTrail.ActionMaxLength),
                EntityType = entityType,
                EntityId = entityId,
                SubjectDisplayName = AuditTrail.Truncate(subjectDisplayName, AuditTrail.SubjectDisplayNameMaxLength),
                // Serialization can throw on a non-serializable descriptor — inside the never-block boundary (M2).
                Snapshot = JsonSerializer.Serialize(payloadOrDescriptor, PayloadJsonOptions),
                // Changes stays null: explicit events carry a descriptor snapshot, never a field delta.
                ActorUserId = actor.ActorUserId,
                UserDisplayName = actor.UserDisplayName,
                UserIp = actor.UserIp,
                SourceType = actor.SourceType,
                SourceId = actor.SourceId,
            };

            if (_sink is not null)
            {
                // Never-block: hand the built row to the out-of-band sink (no tie to the caller's transaction;
                // post-commit ordering is a caller contract). The sink owns its own mapped write context, so
                // the bound-context activation gate does not apply here.
                await _sink.PersistAsync([row], cancellationToken: cancellationToken);
                return;
            }

            // Transitional persisting-safe fallback (no consumer sink registered yet): inline-persist on the
            // bound context, shipped behaviour MINUS the rethrow. The activation gate applies here — a
            // wired-but-unmapped consumer must not silently drop security events.
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

            _context.Add(row);
            await _context.SaveChangesAsync(cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cooperative cancellation: never swallow the caller's own cancellation (precedes the broad catch).
            throw;
        }
        catch (Exception ex)
        {
            // Never-block (M2): NO caller of LogAsync observes an audit-infrastructure exception. A
            // serialization failure, an inline-write failure, or (with a sink) any handoff failure is logged
            // and swallowed — the sink owns its durable retry/deferral; this is the last-resort boundary.
            Logger.LogError(ex,
                "Explicit audit event was swallowed under the never-block invariant. "
                + "Dropped event: Category={Category}, EntityType={EntityType}, EntityId={EntityId}.",
                category, entityType, entityId);
            AuditCaptureDiagnostics.CaptureFailed.Add(1, new KeyValuePair<string, object?>("reason", "explicit-log"));
        }
    }
}

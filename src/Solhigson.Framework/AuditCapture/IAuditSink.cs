using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// The never-block audit-persistence seam (F2-prime / F3-prime, §2 NEVER-BLOCK INVARIANT). Every capture
/// source — the <see cref="AuditCaptureSaveChangesInterceptor"/> (on the TRUE outermost-business-transaction
/// -committed signal) and the explicit <see cref="AuditTrailService{TContext}"/> — hands its already-built
/// <see cref="AuditTrail"/> rows here to be persisted OUT-OF-BAND, on a SEPARATE short-lived scope/DbContext,
/// NEVER the business transaction/connection. An audit-write failure can therefore never abort the business
/// action.
///
/// <para><b>Framework-owned, consumer-implemented.</b> The framework DEFINES this abstraction but does NOT
/// register a concrete implementation for it (there is no framework default in
/// <c>SolhigsonAutofacModule</c>). The consumer provides the implementation (the inline-attempt-then-durable
/// -Hangfire sink) and wires it — the interceptor and the explicit service consume it as an OPTIONAL
/// dependency, so the framework container still closes with no sink registered.</para>
///
/// <para><b>Swallow-all by contract (M2).</b> An implementation MUST NOT let ANY exception propagate to a
/// caller of <see cref="PersistAsync"/>: on any inline-write failure it routes to its durable retry path,
/// and even an enqueue failure is swallowed after emitting a structured log plus a metric. The capture
/// sources additionally guard the call defensively, but the never-block contract is owned here.</para>
///
/// <para><b>Lifetime.</b> Because the sink is captured by the <see cref="AuditCaptureSaveChangesInterceptor"/>
/// (registered <c>SingleInstance</c> — a pooled DbContext captures the interceptor across scopes), an
/// implementation MUST be registered as a SINGLETON that manages its own short-lived write scope/DbContext
/// internally per <see cref="PersistAsync"/> call, NOT injected as a scoped dependency (which would be a
/// captive dependency of the singleton interceptor).</para>
/// </summary>
public interface IAuditSink
{
    /// <summary>
    /// Persists the already-built, already-masked <see cref="AuditTrail"/> rows out-of-band. Called AFTER the
    /// business action has committed (interceptor path) or in the caller's scope with no transaction tie
    /// (explicit-log path). The rows carry their capture-time <see cref="AuditTrail.Id"/>/<see cref="AuditTrail.Created"/>
    /// and their capture-time-masked <see cref="AuditTrail.Snapshot"/>/<see cref="AuditTrail.Changes"/> verbatim;
    /// an implementation MUST NOT regenerate the keys nor re-mask the payload.
    /// </summary>
    /// <param name="rows">
    /// One or more audit rows accumulated across the outermost business transaction (the interceptor path may
    /// batch multiple entities and multiple saves into one handoff; the explicit-log path passes a single row).
    /// </param>
    /// <param name="cancellationToken">
    /// A token the implementation MAY honor on its inline attempt. Note the interceptor's post-commit handoff
    /// passes <see cref="CancellationToken.None"/> deliberately (the business action already committed, so the
    /// out-of-band write must not be abandoned by the caller's token); the explicit-log path forwards the
    /// caller's token.
    /// </param>
    Task PersistAsync(IReadOnlyList<AuditTrail> rows, CancellationToken cancellationToken);
}

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// Explicit audit logging (F3): appends a single <see cref="AuditTrail"/> row for an event that is NOT a
/// data change — a <see cref="AuditEventCategory.SecurityEvent"/> (login, lockout, password change, DSAR,
/// impersonation) or a <see cref="AuditEventCategory.BusinessEvent"/> (export, API call, domain milestone).
/// <see cref="AuditEventCategory.DataChange"/> is owned exclusively by
/// <see cref="AuditCaptureSaveChangesInterceptor"/> and is rejected here.
///
/// <para><b>Binding.</b> <typeparamref name="TContext"/> is the CONSUMER's own <see cref="DbContext"/> —
/// the one whose model maps <see cref="AuditTrail"/> — never the framework's fixed-model contexts. The
/// constraint is deliberately the loosest workable one (<c>where TContext : DbContext</c>; the service
/// needs only <c>Add</c>, <c>SaveChangesAsync</c> and <c>Model.FindEntityType</c>): consumer-not-framework
/// is enforced at RUNTIME by the same <c>Model.FindEntityType(typeof(AuditTrail))</c> activation gate the
/// F2 capture interceptor uses, so binding a context that has not mapped <see cref="AuditTrail"/> (e.g.
/// <c>SolhigsonDbContext</c>) makes every call a logged no-op. Do NOT tighten the constraint to a
/// framework context type — that would re-admit the framework context the gate exists to exclude.</para>
/// </summary>
/// <typeparam name="TContext">The consumer <see cref="DbContext"/> whose model maps <see cref="AuditTrail"/>.</typeparam>
public interface IAuditTrailService<TContext> where TContext : DbContext
{
    /// <summary>
    /// Appends one explicit audit row, participating in any ambient transaction on the bound context
    /// (no owned transaction). Fail-closed: there is deliberately NO broad catch on this path — a failed
    /// write (or a non-serializable <paramref name="payloadOrDescriptor"/>) throws to the caller; any
    /// swallow posture is a caller-side decision.
    /// </summary>
    /// <remarks>
    /// <b>Caution — the F2 field masker does NOT run on this explicit path.</b>
    /// <see cref="AuditFieldMasker"/> guards only the interceptor-captured
    /// <see cref="AuditEventCategory.DataChange"/> payloads; <paramref name="payloadOrDescriptor"/> is
    /// serialized RAW into the append-only <see cref="AuditTrail.Snapshot"/>, where it cannot be redacted
    /// after the fact. Never pass an entity or any secret-bearing object here — pass a controlled
    /// projection/descriptor carrying only the fields the audit row is meant to expose.
    /// </remarks>
    /// <param name="category">
    /// REQUIRED classification: <see cref="AuditEventCategory.SecurityEvent"/> or
    /// <see cref="AuditEventCategory.BusinessEvent"/>. <see cref="AuditEventCategory.DataChange"/> (the
    /// interceptor-owned category, and — footgun — the enum's default value) throws
    /// <see cref="System.ArgumentOutOfRangeException"/>, as does any undefined value.
    /// </param>
    /// <param name="entityType">
    /// The SUBJECT's type name (e.g. the subject user of a login/lockout/password/DSAR event; the TARGET
    /// user of an impersonation event, with the operator carried as <paramref name="actor"/>). Keys the
    /// <c>(EntityType, EntityId, Created)</c> lookup index.
    /// </param>
    /// <param name="entityId">The SUBJECT's (polymorphic) key, e.g. the subject user id.</param>
    /// <param name="actor">
    /// The EXPLICIT actor behind the event; its five fields are stamped onto the row verbatim. Use
    /// <see cref="AuditActor.UnattributedActor"/> when no human/job actor is resolvable.
    /// </param>
    /// <param name="payloadOrDescriptor">
    /// The event payload, serialized into <see cref="AuditTrail.Snapshot"/> as JSON. It SHOULD carry an
    /// event-type discriminator property (e.g. <c>eventType = "login.failed"</c> vs
    /// <c>"login.lockout"</c>) so sibling events on the same subject stay distinguishable.
    /// <see cref="AuditTrail.Changes"/> is always null for explicit events.
    /// </param>
    /// <param name="cancellationToken">Propagated to <c>SaveChangesAsync</c>; observed before any write.</param>
    /// <param name="action">
    /// Optional action label stamped VERBATIM onto <see cref="AuditTrail.Action"/> — pass the event's
    /// <c>eventType</c> (e.g. <c>"login.failed"</c>) so list surfaces can filter without parsing
    /// <see cref="AuditTrail.Snapshot"/>. Never forced lowercase (unlike the interceptor's pinned
    /// <see cref="AuditActions"/> data-change values). Truncated to 128; null leaves the column null.
    /// Trailing-optional AFTER <paramref name="cancellationToken"/> by design: every existing caller
    /// passes the token by name, so extending (not overloading) breaks no call site.
    /// </param>
    /// <param name="subjectDisplayName">
    /// Optional display name of the SUBJECT (never the actor — the actor's name rides
    /// <paramref name="actor"/>), stamped onto <see cref="AuditTrail.SubjectDisplayName"/>.
    /// Truncated to 256; null leaves the column null.
    /// </param>
    Task LogAsync(
        AuditEventCategory category,
        string entityType,
        string entityId,
        AuditActor actor,
        object payloadOrDescriptor,
        CancellationToken cancellationToken,
        string? action = null,
        string? subjectDisplayName = null);
}

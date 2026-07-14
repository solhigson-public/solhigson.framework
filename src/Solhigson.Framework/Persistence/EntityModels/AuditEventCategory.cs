namespace Solhigson.Framework.Persistence.EntityModels;

/// <summary>
/// Top-level classification of an <see cref="AuditTrail"/> row.
/// Named <c>AuditEventCategory</c> (not <c>EventCategory</c>) deliberately to avoid a
/// type-name collision with the consumer's own domain <c>EventCategory</c>.
/// Values are persisted; the explicit numeric assignments are the stable wire contract,
/// so members MUST NOT be reordered or renumbered once shipped.
/// </summary>
public enum AuditEventCategory
{
    /// <summary>A create/update/delete of an audited entity captured by the SaveChanges interceptor.</summary>
    DataChange = 0,

    /// <summary>A security-relevant event (login, lockout, password change, DSAR, impersonation).</summary>
    SecurityEvent = 1,

    /// <summary>An explicitly logged business event (export, API call, domain milestone).</summary>
    BusinessEvent = 2,
}

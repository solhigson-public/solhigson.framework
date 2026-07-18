using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// Opt-in seam through which an audited entity surfaces a human-readable subject display name onto its
/// captured <see cref="AuditTrail.SubjectDisplayName"/> rows.
/// <see cref="AuditCaptureSaveChangesInterceptor"/> binds by TYPE MEMBERSHIP (an <c>is</c> check on the
/// tracked instance) — deliberately NOT an <c>inherit:false</c> attribute probe — so an implementation on
/// a base class binds for every derived entity. A throwing getter is a consumer bug and rides the capture
/// phase's never-block swallow boundary (logged + <c>audit_capture_failed</c> metric, no row for that
/// save's capture, business save unaffected).
/// </summary>
public interface IAuditSubjectNamed
{
    /// <summary>
    /// The subject display name to stamp (truncated to 256 at the stamp site); null when the entity has
    /// no usable name.
    /// </summary>
    string? AuditSubjectDisplayName { get; }
}

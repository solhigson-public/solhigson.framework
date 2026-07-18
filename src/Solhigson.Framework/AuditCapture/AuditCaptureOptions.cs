using System.Collections.Generic;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// Consumer-configurable options for the audit-capture layer (F2). TWO seams exist — one masking, one
/// capture-gating:
/// <para>
/// <b>Masking seam</b> — <see cref="AdditionalSensitiveNamePatterns"/>: an <b>additive-only</b> overlay of
/// extra case-insensitive substrings that force a property value to be masked, layered ON TOP of the
/// framework's fixed default sensitive-name set (<see cref="AuditFieldMasker.DefaultSensitiveNamePatterns"/>)
/// and the <c>[PersonalData]</c> attribute reflection. There is deliberately NO seam that can remove a
/// default pattern or clear an attribute match: masking is a pure OR across the three sources, so the
/// overlay can only ever mask MORE, never un-mask a field the defaults or <c>[PersonalData]</c> already
/// protect. This is the fail-closed posture — a misconfigured overlay cannot leak a sensitive value.
/// </para>
/// <para>
/// <b>Capture-gating seam</b> — <see cref="RequireAttributedActor"/>: gates WHETHER
/// <see cref="AuditCaptureSaveChangesInterceptor"/> materializes DataChange rows for a save at all; it masks
/// nothing. When enabled, a save whose resolved actor carries no user identity emits no DataChange rows.
/// The explicit <see cref="IAuditTrailService{TContext}.LogAsync"/> path is NEVER gated by it.
/// </para>
/// <para>
/// Registered as a singleton in <c>SolhigsonAutofacModule</c> with <c>PreserveExistingDefaults()</c>, so
/// a consumer may register its own pre-populated instance; otherwise the defaults (empty overlay, gate off)
/// apply.
/// </para>
/// </summary>
public sealed class AuditCaptureOptions
{
    /// <summary>
    /// Extra case-insensitive substrings that, when contained in a property name, force that property's
    /// value to be masked in every audit payload. Additive only: adding patterns can never un-mask a
    /// field matched by the fixed defaults or by <c>[PersonalData]</c>.
    /// </summary>
    public ICollection<string> AdditionalSensitiveNamePatterns { get; } = new List<string>();

    /// <summary>
    /// When <c>true</c>, <see cref="AuditCaptureSaveChangesInterceptor"/> materializes DataChange rows ONLY
    /// for saves whose resolved <see cref="AuditActor.ActorUserId"/> is non-null/non-whitespace: a save with
    /// no attributed human actor (background jobs, startup migrations, anonymous requests) emits no rows —
    /// the "not a human action" rule. User identity IS the attribution fact; <see cref="AuditActor.SourceType"/>
    /// is transport and is never consulted. The explicit <see cref="IAuditTrailService{TContext}.LogAsync"/>
    /// path is NEVER gated by this flag (DSAR and other security/business events legitimately log with
    /// <see cref="AuditActor.UnattributedActor"/>). Default <c>false</c>: capture behaviour is unchanged
    /// unless a consumer opts in.
    /// </summary>
    public bool RequireAttributedActor { get; init; }
}

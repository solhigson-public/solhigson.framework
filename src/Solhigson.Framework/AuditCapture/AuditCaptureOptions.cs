using System.Collections.Generic;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// Consumer-configurable options for the fail-closed audit-capture masking layer (F2).
/// <para>
/// The ONLY seam is <see cref="AdditionalSensitiveNamePatterns"/>: an <b>additive-only</b> overlay of
/// extra case-insensitive substrings that force a property value to be masked, layered ON TOP of the
/// framework's fixed default sensitive-name set (<see cref="AuditFieldMasker.DefaultSensitiveNamePatterns"/>)
/// and the <c>[PersonalData]</c> attribute reflection. There is deliberately NO seam that can remove a
/// default pattern or clear an attribute match: masking is a pure OR across the three sources, so the
/// overlay can only ever mask MORE, never un-mask a field the defaults or <c>[PersonalData]</c> already
/// protect. This is the fail-closed posture — a misconfigured overlay cannot leak a sensitive value.
/// </para>
/// <para>
/// Registered as a singleton in <c>SolhigsonAutofacModule</c> with <c>PreserveExistingDefaults()</c>, so
/// a consumer may register its own pre-populated instance; otherwise the default (empty overlay) applies.
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
}

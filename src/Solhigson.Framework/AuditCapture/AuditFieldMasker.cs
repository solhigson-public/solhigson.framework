using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Identity;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// The fail-closed field-masking decision for audit capture (F2, pin R1). A property value is masked
/// when its name/attribute matches ANY of three OR-combined sources:
/// <list type="number">
///   <item>the framework's fixed <see cref="DefaultSensitiveNamePatterns"/> (case-insensitive substring);</item>
///   <item>the property carries <c>[PersonalData]</c> (<see cref="PersonalDataAttribute"/>, reflected from the
///     canonical Microsoft Identity PII map — NEVER a hand-maintained list);</item>
///   <item>the consumer's additive <see cref="AuditCaptureOptions.AdditionalSensitiveNamePatterns"/> overlay.</item>
/// </list>
/// Because the decision is a pure OR, the overlay is strictly additive: it can only ever mask MORE fields,
/// never un-mask one the defaults or <c>[PersonalData]</c> already protect. Masked values render the fixed
/// <see cref="MaskMarker"/> on BOTH sides of an UPDATE <c>{old,new}</c> pair and in INSERT/DELETE snapshots.
/// </summary>
public sealed class AuditFieldMasker
{
    /// <summary>The fixed marker a masked value renders as, on every side of every payload.</summary>
    public const string MaskMarker = "***";

    /// <summary>
    /// The framework's fixed default sensitive-name predicate (pin R1). A property whose name CONTAINS any of
    /// these substrings (case-insensitive) is masked. Deliberately aggressive (fail-closed): over-masking a
    /// non-sensitive field is a lesser harm than leaking a credential/PII value into an audit row.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultSensitiveNamePatterns =
    [
        "password",
        "secret",
        "token",
        "apikey",
        "ssn",
        "pan",
        "cvv",
        "pin",
    ];

    private readonly string[] _overlayPatterns;

    /// <summary>
    /// Builds a masker over the fixed defaults plus the consumer's additive overlay. A null
    /// <paramref name="options"/> yields the defaults-plus-<c>[PersonalData]</c> masker with no overlay.
    /// </summary>
    public AuditFieldMasker(AuditCaptureOptions? options = null)
    {
        _overlayPatterns = options?.AdditionalSensitiveNamePatterns is { } extra
            ? extra.Where(p => !string.IsNullOrEmpty(p)).ToArray()
            : [];
    }

    /// <summary>
    /// True when the property must be masked. <paramref name="propertyName"/> is matched against the fixed
    /// defaults and the additive overlay (case-insensitive substring); <paramref name="propertyInfo"/> (when
    /// resolvable) is reflected for <c>[PersonalData]</c>. Any single match masks (fail-closed OR).
    /// </summary>
    public bool ShouldMask(string propertyName, PropertyInfo? propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        if (propertyInfo?.GetCustomAttribute<PersonalDataAttribute>(inherit: true) is not null)
        {
            return true;
        }

        foreach (var pattern in DefaultSensitiveNamePatterns)
        {
            if (propertyName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var pattern in _overlayPatterns)
        {
            if (propertyName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

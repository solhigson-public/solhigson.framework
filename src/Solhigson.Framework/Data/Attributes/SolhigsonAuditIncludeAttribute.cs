using System;

namespace Solhigson.Framework.Data.Attributes;

/// <summary>
/// Marks a class for inclusion in the framework's same-database audit-trail capture.
/// Framework-owned marker that mirrors the <see cref="CachedPropertyAttribute"/> idiom; it is
/// deliberately NOT Audit.NET's <c>[AuditInclude]</c>, whose use would re-pull
/// <c>Audit.EntityFramework</c> transitively into the framework and make the Audit.NET retirement
/// cosmetic. For framework-owned types that cannot be attributed at their declaration, use the
/// consumer-callable <see cref="Solhigson.Framework.AuditCapture.AuditCaptureRegistry"/> instead.
/// The attribute is deliberately non-inherited: a base-class decoration must not silently opt in
/// every derived entity (per-entity opt-in semantics).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SolhigsonAuditIncludeAttribute : Attribute
{
}

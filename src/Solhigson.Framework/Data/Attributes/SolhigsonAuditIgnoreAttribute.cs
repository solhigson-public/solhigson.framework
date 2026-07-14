using System;

namespace Solhigson.Framework.Data.Attributes;

/// <summary>
/// Marks a class for exclusion from the framework's same-database audit-trail capture.
/// Framework-owned marker that mirrors the <see cref="CachedPropertyAttribute"/> idiom; it is
/// deliberately NOT Audit.NET's <c>[AuditIgnore]</c>. For framework-owned types that cannot be
/// attributed at their declaration, use the consumer-callable
/// <see cref="Solhigson.Framework.AuditCapture.AuditCaptureRegistry"/> instead.
/// Property-level placement excludes a single property of an audited entity from capture (the
/// downstream EntityBase Id/Created/Updated exclusion use-case), and the attribute is deliberately
/// non-inherited (strict per-class opt-out).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false)]
public sealed class SolhigsonAuditIgnoreAttribute : Attribute
{
}

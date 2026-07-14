using System;
using System.Collections.Concurrent;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// Consumer-callable, fluent opt-in registry for <b>framework-owned</b> types that cannot be
/// attributed with <c>[SolhigsonAuditInclude]</c> at their declaration (for example
/// <c>SolhigsonPermission</c> and <c>SolhigsonRolePermission</c>). This is the framework's single
/// opt-in registry — reusing Audit.NET's <c>[AuditInclude]</c> was rejected because it would re-pull
/// <c>Audit.EntityFramework</c> into the framework.
/// <para>
/// Registered in <c>SolhigsonAutofacModule</c> as a singleton (<c>PreserveExistingDefaults()</c>), so
/// the fail-closed SaveChanges capture interceptor (F2) resolves ONE shared instance and queries it
/// via <see cref="IsIncluded(Type)"/>. The class is instantiable so tests exercise a fresh, isolated
/// registry with no ambient/static state.
/// </para>
/// <para>
/// Precedence: <see cref="Ignore{T}"/> wins over <see cref="Include{T}"/> regardless of call order — a
/// type present in the ignore set is never reported included.
/// </para>
/// </summary>
public sealed class AuditCaptureRegistry
{
    private readonly ConcurrentDictionary<Type, byte> _included = new();
    private readonly ConcurrentDictionary<Type, byte> _ignored = new();

    /// <summary>Opts <typeparamref name="T"/> into audit capture. Fluent; returns this registry.</summary>
    public AuditCaptureRegistry Include<T>() where T : class
    {
        _included.TryAdd(typeof(T), 0);
        return this;
    }

    /// <summary>
    /// Opts <typeparamref name="T"/> out of audit capture. Fluent; returns this registry. An ignored
    /// type is never reported included even if also passed to <see cref="Include{T}"/>.
    /// </summary>
    public AuditCaptureRegistry Ignore<T>() where T : class
    {
        _ignored.TryAdd(typeof(T), 0);
        return this;
    }

    /// <summary>
    /// True when <paramref name="type"/> has been opted in and NOT opted out. The query API the
    /// capture interceptor consumes.
    /// </summary>
    public bool IsIncluded(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (_ignored.ContainsKey(type))
        {
            return false;
        }

        return _included.ContainsKey(type);
    }

    /// <summary>
    /// True when <paramref name="type"/> has been explicitly opted OUT via <see cref="Ignore{T}"/>.
    /// <para>
    /// Distinct from <c>!IsIncluded(type)</c>, which conflates a registry-ignored type with a type
    /// that was simply never registered. The F2 capture interceptor needs the distinction to honor
    /// the pinned eligibility precedence "an ignore in EITHER source (attribute or registry) wins
    /// over an include in EITHER source": a registry <see cref="Ignore{T}"/> must beat a class-level
    /// <c>[SolhigsonAuditInclude]</c>, which <see cref="IsIncluded"/> alone cannot express because it
    /// reports <c>false</c> for both the ignored and the absent case.
    /// </para>
    /// </summary>
    public bool IsIgnored(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return _ignored.ContainsKey(type);
    }
}

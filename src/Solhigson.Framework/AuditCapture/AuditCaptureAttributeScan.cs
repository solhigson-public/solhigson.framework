using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Solhigson.Framework.Data.Attributes;
using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// One inert audit-attribute placement: <paramref name="AttributeType"/> sits on <paramref name="InertType"/>, a
/// base class that is NOT itself a mapped entity type, so capture eligibility (resolved on the CONCRETE mapped
/// type only, <c>inherit:false</c>) never reads it. <paramref name="ConcreteTypes"/> are the tracked mapped entity
/// types deriving from it — the types the attribute must move to. One finding per (inert type, attribute) pair.
/// <paramref name="CarriesBothAttributes"/> is true when the same base carries BOTH audit attributes, so the two
/// findings for it give opposite "move" instructions and the reader needs the ignore-wins precedence.
/// </summary>
internal sealed record InertAuditAttributeFinding(
    Type InertType,
    Type AttributeType,
    IReadOnlyList<Type> ConcreteTypes,
    bool CarriesBothAttributes)
{
    /// <summary>The attribute's source spelling (e.g. <c>[SolhigsonAuditInclude]</c>), for the warning text.</summary>
    public string AttributeName => "[" + AttributeType.Name.Replace("Attribute", string.Empty, StringComparison.Ordinal) + "]";
}

/// <summary>Outcome of one call to the interceptor's once-per-model inert-attribute scan.</summary>
internal enum InertAttributeScanOutcome
{
    /// <summary>This call claimed the model, scanned it and logged every finding.</summary>
    Scanned,

    /// <summary>This call claimed the model, but the scan or its logging threw; the throw was logged and swallowed.</summary>
    Swallowed,

    /// <summary>An earlier call already claimed the model; nothing ran (the once-per-model memo).</summary>
    AlreadyClaimed,

    /// <summary>
    /// No USABLE logger: no factory is installed on <see cref="Solhigson.Framework.Logging.LogManager"/> yet, or
    /// binding a logger to it threw (e.g. a disposed factory). The model was NOT claimed: a warning emitted now
    /// would go nowhere and never be repeated. The scan runs at the first gated save with a working logger.
    /// </summary>
    DeferredNoUsableLogger,
}

/// <summary>
/// Inert-attribute startup scan. <c>[SolhigsonAuditInclude]</c> and <c>[SolhigsonAuditIgnore]</c> are both
/// <c>Inherited = false</c>, and <c>AuditCaptureSaveChangesInterceptor.IsCaptureEligible</c> probes the entry's
/// CONCRETE ClrType with <c>inherit:false</c>, so either attribute placed on an unmapped base class is silently
/// ignored: an include there opts nothing in, an ignore there opts nothing out. This scan walks every tracked
/// mapped entity type's base-type chain (up to, not including, <see cref="object"/>) and reports each decorated
/// base that is not itself a mapped entity type.
///
/// <para><b>Scope.</b> A decorated base that IS itself a mapped entity type (a TPH/TPT root) is out of this scan's
/// scope by spec and is never reported, even though an attribute on an ABSTRACT mapped root is equally inert for
/// its derived types (no entry ever has the abstract root as its ClrType). Owned entity types ARE walked: they are
/// tracked and captured like any other entry. Keyless types are never tracked, so they are not walked; property-bag
/// (<c>Dictionary&lt;string, object&gt;</c>) types have no user base chain; <see cref="AuditTrail"/> is
/// hard-excluded from eligibility.</para>
///
/// <para><b>Once per model.</b> The interceptor is a singleton captured across pooled contexts, so the memo is a
/// static <see cref="ConditionalWeakTable{TKey,TValue}"/> keyed by <see cref="IModel"/>, never instance state.
/// <see cref="IsClaimed"/> is the lock-free fast path read on every gated save; <see cref="TryClaim"/> takes the
/// table's lock only until the model is claimed, and claims it atomically so the scan runs at most once.</para>
/// </summary>
internal static class AuditCaptureAttributeScan
{
    private static readonly Type[] ScannedAttributes =
    [
        typeof(SolhigsonAuditIncludeAttribute),
        typeof(SolhigsonAuditIgnoreAttribute),
    ];

    private static readonly object ClaimedMarker = new();

    /// <summary>Models already claimed for their one scan. Weak-keyed, so a discarded model is collectable.</summary>
    private static readonly ConditionalWeakTable<IModel, object> ClaimedModels = [];

    /// <summary>Lock-free: whether <paramref name="model"/> has already been claimed.</summary>
    internal static bool IsClaimed(IModel model) => ClaimedModels.TryGetValue(model, out _);

    /// <summary>
    /// Atomically claims <paramref name="model"/> for its one-time scan: <c>true</c> exactly once per model,
    /// <c>false</c> on every later call. Callers check <see cref="IsClaimed"/> first so the lock is not taken
    /// on every save.
    /// </summary>
    internal static bool TryClaim(IModel model) => ClaimedModels.TryAdd(model, ClaimedMarker);

    /// <summary>
    /// Pure scan over <paramref name="model"/>'s entity types (see the class remarks for what is walked). Output
    /// is ordered deterministically.
    /// </summary>
    internal static IReadOnlyList<InertAuditAttributeFinding> Scan(IModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var mapped = new HashSet<Type>();
        var roots = new HashSet<Type>();
        foreach (var entityType in model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            mapped.Add(clrType);

            if (clrType == typeof(Dictionary<string, object>)
                || clrType == typeof(AuditTrail)
                || entityType.FindPrimaryKey() is null)
            {
                continue;
            }

            roots.Add(clrType);
        }

        return Scan(roots, mapped);
    }

    /// <summary>
    /// Pure scan core: for each concrete type in <paramref name="concreteTypes"/>, walks the full base chain and
    /// reports every base NOT in <paramref name="mappedTypes"/> that carries either audit attribute. Grouped so
    /// each (inert base, attribute) pair is reported once, naming all concrete types beneath it.
    /// </summary>
    internal static IReadOnlyList<InertAuditAttributeFinding> Scan(
        IEnumerable<Type> concreteTypes,
        IReadOnlySet<Type> mappedTypes)
    {
        var grouped = new Dictionary<(Type Inert, Type Attribute), HashSet<Type>>();
        foreach (var concrete in concreteTypes)
        {
            // Walk the WHOLE chain: a mapped intermediate base does not stop the walk, so an unmapped
            // grandparent above it is still found.
            for (var baseType = concrete.BaseType; baseType is not null && baseType != typeof(object); baseType = baseType.BaseType)
            {
                if (mappedTypes.Contains(baseType))
                {
                    continue;
                }

                foreach (var attribute in ScannedAttributes)
                {
                    if (!baseType.IsDefined(attribute, inherit: false))
                    {
                        continue;
                    }

                    if (!grouped.TryGetValue((baseType, attribute), out var concretes))
                    {
                        grouped[(baseType, attribute)] = concretes = [];
                    }

                    concretes.Add(concrete);
                }
            }
        }

        return grouped
            .Select(g => new InertAuditAttributeFinding(
                g.Key.Inert,
                g.Key.Attribute,
                g.Value.OrderBy(t => t.FullName, StringComparer.Ordinal).ToList(),
                ScannedAttributes.All(a => g.Key.Inert.IsDefined(a, inherit: false))))
            .OrderBy(f => f.InertType.FullName, StringComparer.Ordinal)
            .ThenBy(f => f.AttributeType.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Readable type name for the warning text. <see cref="Type.FullName"/> of a CLOSED generic embeds each type
    /// argument's assembly-qualified name; this renders <c>Namespace.Base&lt;Namespace.Arg&gt;</c> instead.
    /// </summary>
    internal static string DisplayName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        // Strip every arity marker (`1), including one on an enclosing generic type of a nested type.
        var definitionName = Regex.Replace(type.GetGenericTypeDefinition().FullName ?? type.Name, @"`\d+", string.Empty);

        return definitionName + "<" + string.Join(", ", type.GetGenericArguments().Select(DisplayName)) + ">";
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using Solhigson.Framework.Logging;

namespace Solhigson.Framework.Web.Middleware;

/// <summary>
/// One compiled inbound-trace path pattern. Segments are split on <c>/</c> with empty segments
/// dropped; a segment of the form <c>{name}</c> matches exactly one request-path segment (the name
/// carries no meaning); every other segment matches its request-path segment ignoring case. A pattern
/// covers the path it names and everything beneath it, by segment, so <c>/api</c> matches
/// <c>/api/orders</c> and never <c>/apiary</c>.
/// </summary>
internal sealed class InboundTracePathPattern
{
    private const string PlaceholderKey = "{}";

    // null entry = placeholder; otherwise the literal segment.
    private readonly string?[] _segments;

    private InboundTracePathPattern(string?[] segments, string key)
    {
        _segments = segments;
        Key = key;
    }

    /// <summary>
    /// Normalised form: leading <c>/</c>, empty segments dropped, placeholder names erased. Two
    /// patterns with the same key (ignoring case) match exactly the same paths.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Returns the normalised key for a raw pattern, or null for a null or blank entry (ignored, never
    /// read as "match everything").
    /// </summary>
    public static string? Normalise(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var parts = raw.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            if (IsPlaceholder(parts[i]))
            {
                parts[i] = PlaceholderKey;
            }
        }

        return "/" + string.Join('/', parts);
    }

    /// <summary>Builds a pattern from a key produced by <see cref="Normalise"/>.</summary>
    public static InboundTracePathPattern FromKey(string key)
    {
        var parts = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var segments = new string?[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            segments[i] = parts[i] == PlaceholderKey ? null : parts[i];
        }

        return new InboundTracePathPattern(segments, key);
    }

    /// <summary>Allocation-free: walks the path segment by segment.</summary>
    public bool Matches(ReadOnlySpan<char> path)
    {
        var position = 0;
        foreach (var segment in _segments)
        {
            if (!TryReadSegment(path, ref position, out var pathSegment))
            {
                return false;
            }

            if (segment is not null && !pathSegment.Equals(segment, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPlaceholder(string segment) =>
        segment.Length >= 2 && segment[0] == '{' && segment[^1] == '}';

    private static bool TryReadSegment(ReadOnlySpan<char> path, ref int position, out ReadOnlySpan<char> segment)
    {
        while (position < path.Length && path[position] == '/')
        {
            position++;
        }

        if (position >= path.Length)
        {
            segment = default;
            return false;
        }

        var start = position;
        while (position < path.Length && path[position] != '/')
        {
            position++;
        }

        segment = path[start..position];
        return true;
    }
}

/// <summary>
/// The compiled form of one pattern list, cached against the instance the consumer's function last
/// returned. The same instance is a reference check; a different instance is compared to the cached
/// patterns as a case-insensitive set of normalised keys and the patterns are rebuilt only when that
/// set differs. Never compiles per request while the list is unchanged. Lock-free: the snapshot is
/// immutable and swapped whole, so a race costs at most one redundant rebuild.
/// </summary>
internal sealed class InboundTracePathPatterns
{
    private sealed class Snapshot(
        IReadOnlyCollection<string>? source, HashSet<string> keys, InboundTracePathPattern[] patterns)
    {
        public IReadOnlyCollection<string>? Source { get; } = source;
        public HashSet<string> Keys { get; } = keys;
        public InboundTracePathPattern[] Patterns { get; } = patterns;
    }

    private readonly string _listName;
    private readonly LogWrapper? _logger;
    private Snapshot _current = new(null, new HashSet<string>(StringComparer.OrdinalIgnoreCase), []);
    private int _compileCount;

    /// <param name="listName">"include" or "exclude", for the warning text only.</param>
    /// <param name="logger">Receives one warning per suspicious entry, on rebuild only.</param>
    public InboundTracePathPatterns(string listName = "", LogWrapper? logger = null)
    {
        _listName = listName;
        _logger = logger;
    }

    /// <summary>Number of times the pattern array has been rebuilt. Test observation only.</summary>
    internal int CompileCount => Volatile.Read(ref _compileCount);

    /// <summary>The currently cached compiled patterns. Test observation only.</summary>
    internal InboundTracePathPattern[] CurrentPatterns => Volatile.Read(ref _current).Patterns;

    public bool AnyMatch(IReadOnlyCollection<string>? source, ReadOnlySpan<char> path)
    {
        foreach (var pattern in Resolve(source))
        {
            if (pattern.Matches(path))
            {
                return true;
            }
        }

        return false;
    }

    internal InboundTracePathPattern[] Resolve(IReadOnlyCollection<string>? source)
    {
        var current = Volatile.Read(ref _current);
        if (ReferenceEquals(source, current.Source))
        {
            return current.Patterns;
        }

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string>? suspicious = null;
        if (source is not null)
        {
            foreach (var raw in source)
            {
                var key = InboundTracePathPattern.Normalise(raw);
                if (key is null)
                {
                    continue;
                }

                keys.Add(key);
                if (IsSuspicious(raw!))
                {
                    (suspicious ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(raw!.Trim());
                }
            }
        }

        if (keys.SetEquals(current.Keys))
        {
            // Same contents in a new instance: keep the compiled patterns, remember the instance so the
            // next call with it is a reference check.
            Volatile.Write(ref _current, new Snapshot(source, current.Keys, current.Patterns));
            return current.Patterns;
        }

        var patterns = new InboundTracePathPattern[keys.Count];
        var index = 0;
        foreach (var key in keys)
        {
            patterns[index++] = InboundTracePathPattern.FromKey(key);
        }

        Interlocked.Increment(ref _compileCount);
        Volatile.Write(ref _current, new Snapshot(source, keys, patterns));

        // Here, on rebuild, and never per request: an unchanged list is not re-warned.
        if (suspicious is not null && _logger is not null)
        {
            foreach (var entry in suspicious)
            {
                _logger.LogWarning(
                    "Inbound trace {list} pattern {pattern} contains '*', '?', '://' or '%'. Patterns are matched " +
                    "literally, by segment, against the decoded request path only, so this entry will likely " +
                    "never match. Use a plain path pattern such as /api/orders.",
                    _listName, entry);
            }
        }

        return patterns;
    }

    /// <summary>
    /// A wildcard, a scheme or host, a query string or percent-encoding: none can match the decoded
    /// request path that is compared, so the entry is almost certainly a mistake.
    /// </summary>
    internal static bool IsSuspicious(string raw) =>
        raw.Contains('*') || raw.Contains('?') || raw.Contains("://", StringComparison.Ordinal) || raw.Contains('%');
}

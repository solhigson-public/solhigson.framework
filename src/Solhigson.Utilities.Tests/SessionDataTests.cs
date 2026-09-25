using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Shouldly;
using Xunit;

namespace Solhigson.Utilities.Tests;

/// <summary>
/// Pins that HelperFunctions session helpers store and serve data ONLY through the HTTP session,
/// never through thread named data slots (which leak across requests and jobs on pooled threads).
/// </summary>
public class SessionDataTests
{
    private sealed class StubSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public IEnumerable<string> Keys => _store.Keys;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Clear() => _store.Clear();
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) =>
            _store.TryGetValue(key, out value);
    }

    private sealed class StubSessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }

    private static DefaultHttpContext ContextWithSession()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<ISessionFeature>(new StubSessionFeature { Session = new StubSession() });
        return context;
    }

    private static string NewKey() => "session-test-" + Guid.NewGuid().ToString("N");

    // Synchronous on purpose: write and reads stay on the same thread, so a thread-slot
    // fallback (the removed behaviour) would store the value and serve it back, failing this test.
    // All observations are taken first and asserted together so each one reports independently.
    [Fact]
    public void SafeSetSessionData_NullContext_DoesNotLeakToSameThreadReads()
    {
        var key = NewKey();

        HelperFunctions.SafeSetSessionData(key, "leaked-value", (HttpContext?)null);

        // Test-only use of the slot API: pins that the null-context write stores nothing in a thread slot.
        var slotValue = Thread.GetData(Thread.GetNamedDataSlot(key));
        // Live session that lacks the key: must not fall back to a thread slot.
        var liveSessionRead = HelperFunctions.SafeGetSessionData(key, ContextWithSession());
        // Null context: must not fall back to a thread slot.
        var nullContextRead = HelperFunctions.SafeGetSessionData(key, (HttpContext?)null);
        // No session feature: .Session throws; the helper must swallow it and return null.
        var noSessionFeatureRead = HelperFunctions.SafeGetSessionData(key, new DefaultHttpContext());

        key.ShouldSatisfyAllConditions(
            () => slotValue.ShouldBeNull(),
            () => liveSessionRead.ShouldBeNull(),
            () => nullContextRead.ShouldBeNull(),
            () => noSessionFeatureRead.ShouldBeNull());
    }

    [Fact]
    public void SafeSetSessionData_SessionContext_RoundTrips()
    {
        var key = NewKey();
        var context = ContextWithSession();

        HelperFunctions.SafeSetSessionData(key, "session-value", context);

        HelperFunctions.SafeGetSessionData(key, context).ShouldBe("session-value");
    }

    [Fact]
    public void SafeRemoveSessionData_NullContext_DoesNotThrowAndLeavesSessionValueIntact()
    {
        var key = NewKey();
        var context = ContextWithSession();
        HelperFunctions.SafeSetSessionData(key, "session-value", context);

        Should.NotThrow(() => HelperFunctions.SafeRemoveSessionData(key, (HttpContext?)null));

        HelperFunctions.SafeGetSessionData(key, context).ShouldBe("session-value");
    }
}

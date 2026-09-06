using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Solhigson.Framework.Throttling;
using Xunit;

namespace Solhigson.Framework.Tests;

/// <summary>
/// The throttle bucket key. These are the first throttling tests in this repository; they exist
/// because <see cref="ThrottleKeyExtractor.GetClientIp"/> used to return the LEFTMOST entry of the
/// raw <c>X-Forwarded-For</c> header, which is caller-written text — so any caller could choose
/// their own bucket, choose a fresh one per request, or name someone else's address. The key is
/// now <c>Connection.RemoteIpAddress</c>, the address the host's forwarded-headers middleware
/// resolved, and the tests below are what stops a header read from being reintroduced.
/// </summary>
public class ThrottleKeyExtractorTests
{
    private const string RemoteAddress = "203.0.113.7";
    private const string ForgedAddress = "198.51.100.99";

    // ---------------------------------------------------------------------------------------
    // The bypass itself: a forwarded header never moves the bucket.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// The core regression. A caller sends <c>X-Forwarded-For</c> naming a DIFFERENT address; the
    /// key must still be the connection's address. Before the fix this returned
    /// <c>ip:198.51.100.99</c> — the forged value — which is the bypass.
    /// </summary>
    [Fact]
    public void GetClientKey_ForwardedHeaderNamesAnotherAddress_KeysOnTheRemoteAddress()
    {
        var context = BuildContext(RemoteAddress);
        context.Request.Headers["X-Forwarded-For"] = ForgedAddress;

        ThrottleKeyExtractor.GetClientKey(context).ShouldBe($"ip:{RemoteAddress}");
        ThrottleKeyExtractor.GetClientIp(context).ShouldBe(RemoteAddress);
    }

    /// <summary>
    /// The same request with no header at all: identical key. Paired with the test above, this is
    /// the property that matters — the header is not an input.
    /// </summary>
    [Fact]
    public void GetClientKey_NoForwardedHeader_KeysOnTheRemoteAddress()
    {
        var context = BuildContext(RemoteAddress);

        context.Request.Headers.ContainsKey("X-Forwarded-For").ShouldBeFalse();
        ThrottleKeyExtractor.GetClientKey(context).ShouldBe($"ip:{RemoteAddress}");
    }

    /// <summary>
    /// The escape the old code allowed: a caller rotating the header on every request landed in a
    /// fresh bucket every time. Three different forged headers from one connection must now
    /// produce one key, so all three requests spend the same budget.
    /// </summary>
    [Fact]
    public void GetClientKey_RotatingForgedHeaders_AllLandInTheSameBucket()
    {
        var keys = new HashSet<string>();
        foreach (var forged in new[] { "198.51.100.1", "198.51.100.2", "198.51.100.3" })
        {
            var context = BuildContext(RemoteAddress);
            context.Request.Headers["X-Forwarded-For"] = forged;
            keys.Add(ThrottleKeyExtractor.GetClientKey(context));
        }

        keys.ShouldHaveSingleItem().ShouldBe($"ip:{RemoteAddress}");
    }

    /// <summary>
    /// A multi-entry header — the realistic proxy-chain shape. Neither the leftmost entry (the old
    /// return value) nor the rightmost is chosen; entry selection is the host's job, not ours.
    /// </summary>
    [Fact]
    public void GetClientIp_MultiEntryForwardedHeader_PicksNeitherEnd()
    {
        var context = BuildContext(RemoteAddress);
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.99, 192.0.2.10, 192.0.2.20";

        ThrottleKeyExtractor.GetClientIp(context).ShouldBe(RemoteAddress);
    }

    /// <summary>
    /// An empty / whitespace header value must not degrade the key either — no branch on the
    /// header exists to fall out of.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",")]
    public void GetClientIp_EmptyOrDegenerateForwardedHeader_StillTheRemoteAddress(string headerValue)
    {
        var context = BuildContext(RemoteAddress);
        context.Request.Headers["X-Forwarded-For"] = headerValue;

        ThrottleKeyExtractor.GetClientIp(context).ShouldBe(RemoteAddress);
    }

    /// <summary>
    /// Every forwarding header a future "improvement" might reach for is ignored, not just
    /// <c>X-Forwarded-For</c>. Sent all at once, the key is unchanged.
    /// </summary>
    [Fact]
    public void GetClientIp_NoForwardingHeaderOfAnyNameIsRead()
    {
        var context = BuildContext(RemoteAddress);
        context.Request.Headers["X-Forwarded-For"] = ForgedAddress;
        context.Request.Headers["X-Real-IP"] = ForgedAddress;
        context.Request.Headers["CF-Connecting-IP"] = ForgedAddress;
        context.Request.Headers["True-Client-IP"] = ForgedAddress;
        context.Request.Headers["X-Client-IP"] = ForgedAddress;
        context.Request.Headers["Forwarded"] = $"for={ForgedAddress}";
        // What Azure App Service actually presents to the app, so the first thing an Azure-hosted
        // consumer reaches for.
        context.Request.Headers["X-Azure-ClientIP"] = ForgedAddress;
        context.Request.Headers["X-Azure-SocketIP"] = ForgedAddress;
        // What ForwardedHeadersMiddleware itself WRITES when it consumes entries — which means a
        // caller can send it whenever that middleware is not registered, and it looks trustworthy
        // precisely because the framework's own middleware is its usual author.
        context.Request.Headers["X-Original-For"] = ForgedAddress;

        ThrottleKeyExtractor.GetClientIp(context).ShouldBe(RemoteAddress);
        ThrottleKeyExtractor.GetClientKey(context).ShouldBe($"ip:{RemoteAddress}");
    }

    /// <summary>
    /// THE TRUSTED-LOOKING-REMOTE VARIANT — do not delete this as redundant with the public-address
    /// facts above; it is the only test that catches the reintroduction this class most likely
    /// faces.
    /// </summary>
    /// <remarks>
    /// The header read does not usually come back unconditionally. It comes back GUARDED, as
    /// "only trust the header when the connection is local", i.e.
    /// <c>if (remote.IsLoopback || IsPrivate(remote)) use X-Forwarded-For;</c> — which reads as a
    /// careful improvement and passes every other test in this class, because every other test
    /// arranges a PUBLIC remote (TEST-NET / documentation addresses). That form is exactly as
    /// bypassable as the original in the deployment the header exists for: behind a local reverse
    /// proxy the remote address IS loopback or RFC1918 on every request, so the guard is always
    /// open and every caller picks their own bucket again. Hence a private/loopback remote with a
    /// forged header, asserting the remote still wins.
    /// </remarks>
    [Theory]
    [InlineData("127.0.0.1")]   // loopback, IPv4 — sidecar / same-host reverse proxy
    [InlineData("::1")]         // loopback, IPv6 — same, dual-stack host
    [InlineData("10.0.0.5")]    // RFC1918 /8  — proxy on the same VNet
    [InlineData("192.168.1.9")] // RFC1918 /16 — proxy on the same LAN
    [InlineData("172.16.0.2")]  // RFC1918 /12 — the range a hand-rolled IsPrivate most often bungles
    public void GetClientIp_TrustedLookingRemoteWithForgedHeader_StillTheRemoteAddress(string remoteAddress)
    {
        var context = BuildContext(remoteAddress);
        context.Request.Headers["X-Forwarded-For"] = ForgedAddress;

        ThrottleKeyExtractor.GetClientIp(context).ShouldBe(remoteAddress);
        ThrottleKeyExtractor.GetClientKey(context).ShouldBe($"ip:{remoteAddress}");
    }

    /// <summary>
    /// An IPv6 caller round-trips as its own address, so IPv6 clients are not all collapsed into
    /// one bucket.
    /// </summary>
    [Fact]
    public void GetClientIp_IpV6RemoteAddress_IsKeyedAsItself()
    {
        var context = BuildContext("2001:db8::1");
        context.Request.Headers["X-Forwarded-For"] = ForgedAddress;

        ThrottleKeyExtractor.GetClientIp(context).ShouldBe("2001:db8::1");
    }

    // ---------------------------------------------------------------------------------------
    // The null-address fallback.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// No remote address (non-TCP transports, unit tests) collapses to one shared
    /// <c>"unknown"</c> bucket — the fail-closed direction. Critically, a header present on such a
    /// request must NOT be used as a fallback: that would hand the bypass back to any caller who
    /// can reach a transport with no address.
    /// </summary>
    [Fact]
    public void GetClientIp_NullRemoteAddress_ReturnsUnknownAndIgnoresTheHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = ForgedAddress;

        context.Connection.RemoteIpAddress.ShouldBeNull();
        ThrottleKeyExtractor.GetClientIp(context).ShouldBe("unknown");
        ThrottleKeyExtractor.GetClientKey(context).ShouldBe("ip:unknown");
    }

    // ---------------------------------------------------------------------------------------
    // The authenticated branch, unchanged by this work and pinned so it stays that way.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// An authenticated caller keys on their identity, not their address, so a signed-in user
    /// keeps one bucket across a changing address.
    /// </summary>
    [Fact]
    public void GetClientKey_AuthenticatedWithNameIdentifier_KeysOnTheUser()
    {
        var context = BuildContext(RemoteAddress, Authenticated(new Claim(ClaimTypes.NameIdentifier, "user-1")));
        context.Request.Headers["X-Forwarded-For"] = ForgedAddress;

        ThrottleKeyExtractor.GetClientKey(context).ShouldBe("user:user-1");
    }

    /// <summary>
    /// The JWT-shaped alternative: a bare <c>sub</c> claim with no <c>NameIdentifier</c> is
    /// accepted too.
    /// </summary>
    [Fact]
    public void GetClientKey_AuthenticatedWithSubClaimOnly_KeysOnTheUser()
    {
        var context = BuildContext(RemoteAddress, Authenticated(new Claim("sub", "user-2")));

        ThrottleKeyExtractor.GetClientKey(context).ShouldBe("user:user-2");
    }

    /// <summary>
    /// Claim precedence when both are present: <c>NameIdentifier</c> wins. Pinned so a reordering
    /// cannot silently split one user across two buckets.
    /// </summary>
    [Fact]
    public void GetClientKey_BothClaimsPresent_NameIdentifierWins()
    {
        var context = BuildContext(
            RemoteAddress,
            Authenticated(new Claim("sub", "user-sub"), new Claim(ClaimTypes.NameIdentifier, "user-nameid")));

        ThrottleKeyExtractor.GetClientKey(context).ShouldBe("user:user-nameid");
    }

    /// <summary>
    /// An authenticated principal carrying neither claim falls through to the IP branch — and that
    /// branch is still the remote address, not the header.
    /// </summary>
    [Fact]
    public void GetClientKey_AuthenticatedWithNeitherClaim_FallsThroughToTheRemoteAddress()
    {
        var context = BuildContext(RemoteAddress, Authenticated(new Claim(ClaimTypes.Email, "a@b.c")));
        context.Request.Headers["X-Forwarded-For"] = ForgedAddress;

        ThrottleKeyExtractor.GetClientKey(context).ShouldBe($"ip:{RemoteAddress}");
    }

    /// <summary>
    /// An empty claim VALUE is treated as absent (<c>string.IsNullOrEmpty</c>), so it cannot
    /// create a shared <c>user:</c> bucket for every such caller.
    /// </summary>
    [Fact]
    public void GetClientKey_AuthenticatedWithEmptyIdentifier_FallsThroughToTheRemoteAddress()
    {
        var context = BuildContext(RemoteAddress, Authenticated(new Claim(ClaimTypes.NameIdentifier, "")));

        ThrottleKeyExtractor.GetClientKey(context).ShouldBe($"ip:{RemoteAddress}");
    }

    /// <summary>
    /// An unauthenticated principal carrying an identifier claim is NOT trusted as a user — an
    /// anonymous caller cannot claim a user bucket.
    /// </summary>
    [Fact]
    public void GetClientKey_UnauthenticatedPrincipalWithClaims_KeysOnTheRemoteAddress()
    {
        var context = BuildContext(RemoteAddress);
        // No authentication type => IsAuthenticated is false.
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-3")]));

        context.User.Identity!.IsAuthenticated.ShouldBeFalse();
        ThrottleKeyExtractor.GetClientKey(context).ShouldBe($"ip:{RemoteAddress}");
    }

    private static DefaultHttpContext BuildContext(string remoteAddress)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteAddress);
        return context;
    }

    private static DefaultHttpContext BuildContext(string remoteAddress, ClaimsPrincipal user)
    {
        var context = BuildContext(remoteAddress);
        context.User = user;
        return context;
    }

    private static ClaimsPrincipal Authenticated(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
}

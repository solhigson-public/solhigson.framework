using System.Collections.Generic;
using System.Text.Json;
using Shouldly;
using Solhigson.Framework.Logging;
using Xunit;

namespace Solhigson.Framework.Tests;

public class ApiTraceDataTests
{
    [Fact]
    public void GetUserIdentity_HeaderPresent_ReturnsValue()
    {
        var data = new ApiTraceData
        {
            RequestHeaders = new Dictionary<string, string>
            {
                { ApiTraceData.UserHttpHeaderIdentifier, "user@example.com" }
            }
        };

        data.GetUserIdentity().ShouldBe("user@example.com");
    }

    [Fact]
    public void GetUserIdentity_HeaderMissing_ReturnsNull()
    {
        var data = new ApiTraceData
        {
            RequestHeaders = new Dictionary<string, string>
            {
                { "other-header", "value" }
            }
        };

        data.GetUserIdentity().ShouldBeNull();
    }

    [Fact]
    public void GetUserIdentity_NullHeaders_ReturnsNull()
    {
        var data = new ApiTraceData { RequestHeaders = null! };

        data.GetUserIdentity().ShouldBeNull();
    }

    [Fact]
    public void Serialization_UrlExcluded_ViaJsonIgnore()
    {
        var data = new ApiTraceData
        {
            Url = "https://example.com/api",
            Method = "GET",
            RequestHeaders = new Dictionary<string, string>(),
        };

        var json = JsonSerializer.Serialize(data);

        json.ShouldNotContain("\"url\"", Case.Insensitive);
        json.ShouldNotContain("https://example.com/api");
    }

    [Fact]
    public void Serialization_DictionaryHeaders_RoundTrips()
    {
        var data = new ApiTraceData
        {
            RequestHeaders = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Authorization", "Bearer token123" }
            }
        };

        var json = JsonSerializer.Serialize(data);
        var deserialized = JsonSerializer.Deserialize<ApiTraceData>(json);

        deserialized.ShouldNotBeNull();
        deserialized.RequestHeaders.ShouldNotBeNull();
        deserialized.RequestHeaders["Content-Type"].ShouldBe("application/json");
        deserialized.RequestHeaders["Authorization"].ShouldBe("Bearer token123");
    }

    [Fact]
    public void Serialization_NullResponseHeaders_SerializesAsNull()
    {
        var data = new ApiTraceData
        {
            RequestHeaders = new Dictionary<string, string>(),
            ResponseHeaders = null,
        };

        var json = JsonSerializer.Serialize(data);

        json.ShouldContain("\"responseHeaders\":null", Case.Insensitive);
    }
}

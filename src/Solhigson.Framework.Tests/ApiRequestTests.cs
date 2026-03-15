using System;
using System.Collections.Generic;
using System.Net.Http;
using Shouldly;
using Solhigson.Framework.Web.Api;
using Xunit;

namespace Solhigson.Framework.Tests;

public class ApiRequestTests
{
    private const string TestUrl = "https://example.com/api";

    [Fact]
    public void Get_SetsHttpMethodGet()
    {
        ApiRequest.Get(TestUrl).HttpMethod.ShouldBe(HttpMethod.Get);
    }

    [Fact]
    public void Post_SetsHttpMethodPost()
    {
        ApiRequest.Post(TestUrl).HttpMethod.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    public void Put_SetsHttpMethodPut()
    {
        ApiRequest.Put(TestUrl).HttpMethod.ShouldBe(HttpMethod.Put);
    }

    [Fact]
    public void Delete_SetsHttpMethodDelete()
    {
        ApiRequest.Delete(TestUrl).HttpMethod.ShouldBe(HttpMethod.Delete);
    }

    [Fact]
    public void Patch_SetsHttpMethodPatch()
    {
        ApiRequest.Patch(TestUrl).HttpMethod.ShouldBe(HttpMethod.Patch);
    }

    [Fact]
    public void Factory_SetsUri()
    {
        ApiRequest.Get(TestUrl).Uri.ShouldBe(new Uri(TestUrl));
    }

    [Fact]
    public void Factory_InvalidUri_ThrowsUriFormatException()
    {
        Should.Throw<UriFormatException>(() => ApiRequest.Get("not-a-uri"));
    }

    [Fact]
    public void Default_FormatIsJson()
    {
        ApiRequest.Get(TestUrl).Format.ShouldBe(ContentTypes.Json);
    }

    [Fact]
    public void Default_ExpectContinueIsFalse()
    {
        ApiRequest.Get(TestUrl).ExpectContinue.ShouldBeFalse();
    }

    [Fact]
    public void Default_ReadResponseContentIsTrue()
    {
        ApiRequest.Get(TestUrl).ReadResponseContent.ShouldBeTrue();
    }

    [Fact]
    public void Default_NullBody_PayloadIsNull()
    {
        ApiRequest.Get(TestUrl).Payload.ShouldBeNull();
    }

    [Fact]
    public void Post_StringBody_PassesThrough()
    {
        var raw = "{\"already\":\"serialized\"}";
        ApiRequest.Post(TestUrl, raw).Payload.ShouldBe(raw);
    }

    [Fact]
    public void Post_ObjectBody_SerializesToJson()
    {
        var body = new { name = "test", value = 42 };
        var request = ApiRequest.Post(TestUrl, body);

        request.Payload.ShouldNotBeNull();
        request.Payload.ShouldContain("\"name\"");
        request.Payload.ShouldContain("\"value\"");
        request.Format.ShouldBe(ContentTypes.Json);
    }

    [Fact]
    public void Post_DictionaryBody_EncodesAsFormUrl()
    {
        var formData = new Dictionary<string, string>
        {
            { "key", "value" },
            { "other", "data" }
        };
        var request = ApiRequest.Post(TestUrl, formData);

        request.Format.ShouldBe(ContentTypes.FormUrlEncoded);
        request.Payload.ShouldNotBeNull();
        request.Payload.ShouldContain("key=value");
        request.Payload.ShouldContain("other=data");
    }

    [Fact]
    public void WithHeader_AddsHeader()
    {
        var request = ApiRequest.Get(TestUrl).WithHeader("X-Custom", "val");

        request.Headers.ShouldNotBeNull();
        request.Headers.ShouldContainKeyAndValue("X-Custom", "val");
    }

    [Fact]
    public void WithBearerToken_AddsAuthorizationHeader()
    {
        var request = ApiRequest.Get(TestUrl).WithBearerToken("tok123");

        request.Headers.ShouldNotBeNull();
        request.Headers["Authorization"].ShouldBe("Bearer tok123");
    }

    [Fact]
    public void WithTimeout_SetsTimeOut()
    {
        ApiRequest.Get(TestUrl).WithTimeout(30).TimeOut.ShouldBe(30);
    }

    [Fact]
    public void AsFormUrlEncoded_SetsFormat()
    {
        ApiRequest.Get(TestUrl).AsFormUrlEncoded().Format.ShouldBe(ContentTypes.FormUrlEncoded);
    }

    [Fact]
    public void AsXml_SetsFormat()
    {
        ApiRequest.Get(TestUrl).AsXml().Format.ShouldBe(ContentTypes.Xml);
    }

    [Fact]
    public void WithoutResponseContent_SetsFalse()
    {
        ApiRequest.Get(TestUrl).WithoutResponseContent().ReadResponseContent.ShouldBeFalse();
    }
}

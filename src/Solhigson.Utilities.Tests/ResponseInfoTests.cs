using System.Text.Json;
using System.Text.Json.Nodes;
using Shouldly;
using Solhigson.Utilities.Dto;
using Xunit;

namespace Solhigson.Utilities.Tests;

public class ResponseInfoTests
{
    [Fact]
    public void ResponseInfo_Serialization_ProducesCamelCaseJson()
    {
        var info = ResponseInfo.SuccessResult("OK");

        var json = info.SerializeToJson();

        json.ShouldNotBeNull();
        // Verify camelCase property names
        json.ShouldContain("\"statusCode\"");
        json.ShouldContain("\"message\"");
        json.ShouldContain("\"data\""); // always present, value is null
        // Should NOT have PascalCase keys
        json.ShouldNotContain("\"StatusCode\"", Case.Sensitive);
        json.ShouldNotContain("\"Message\"", Case.Sensitive);
    }

    [Fact]
    public void ResponseInfo_DefaultState_HasUnexpectedError()
    {
        var info = new ResponseInfo();

        info.StatusCode.ShouldBe(StatusCode.UnExpectedError);
        info.Message.ShouldBe(ResponseInfo.DefaultMessage);
        info.IsSuccessful.ShouldBeFalse();
    }

    [Fact]
    public void ResponseInfo_SuccessResult_SetsStatusCodeAndMessage()
    {
        var info = ResponseInfo.SuccessResult("All good");

        info.StatusCode.ShouldBe(StatusCode.Successful);
        info.Message.ShouldBe("All good");
        info.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void ResponseInfo_FailedResult_SetsStatusCodeAndMessage()
    {
        var info = ResponseInfo.FailedResult("Something broke", "99999");

        info.StatusCode.ShouldBe("99999");
        info.Message.ShouldBe("Something broke");
        info.IsSuccessful.ShouldBeFalse();
    }

    [Fact]
    public void ResponseInfoT_Serialization_IncludesData()
    {
        var info = ResponseInfo.SuccessResult(new { Id = 1, Name = "Test" }, "OK");

        var json = info.SerializeToJson();

        json.ShouldNotBeNull();
        var parsed = JsonNode.Parse(json)!.AsObject();
        parsed["data"].ShouldNotBeNull();
        parsed["data"]!["id"]!.GetValue<int>().ShouldBe(1);
        parsed["data"]!["name"]!.GetValue<string>().ShouldBe("Test");
    }

    [Fact]
    public void ResponseInfoT_SuccessResult_SetsData()
    {
        var data = new List<string> { "a", "b" };
        var info = ResponseInfo.SuccessResult(data);

        info.IsSuccessful.ShouldBeTrue();
        info.Data.ShouldNotBeNull();
        info.Data.Count.ShouldBe(2);
    }

    [Fact]
    public void ResponseInfoT_FailedResult_DefaultData()
    {
        var info = ResponseInfo.FailedResult<string>("Error");

        info.IsSuccessful.ShouldBeFalse();
        info.Data.ShouldBeNull();
    }

    [Fact]
    public void ResponseInfoT_JsonIgnore_ExcludesErrorData()
    {
        var info = ResponseInfo.FailedResult<string>("Error");
        info.ErrorData = new { Detail = "internal" };

        var json = info.SerializeToJson();

        json.ShouldNotBeNull();
        json.ShouldNotContain("errorData");
        json.ShouldNotContain("ErrorData");
        json.ShouldNotContain("internal");
    }
}

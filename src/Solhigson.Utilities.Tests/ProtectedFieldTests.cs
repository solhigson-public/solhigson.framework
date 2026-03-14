using System.Text.Json.Nodes;
using Shouldly;
using Xunit;

namespace Solhigson.Utilities.Tests;

public class ProtectedFieldTests
{
    private static readonly string[] ProtectedFields = ["password", "token", "secret"];

    // --- String overload ---

    [Fact]
    public void CheckForProtectedFields_String_MasksMatchingField()
    {
        var json = """{"password":"secret123","name":"John"}""";

        var result = HelperFunctions.CheckForProtectedFields(json, ProtectedFields);

        result.ShouldNotBeNull();
        var parsed = JsonNode.Parse(result)!.AsObject();
        parsed["password"]!.GetValue<string>().ShouldBe("******");
        parsed["name"]!.GetValue<string>().ShouldBe("John");
    }

    [Fact]
    public void CheckForProtectedFields_String_CaseInsensitive()
    {
        var json = """{"Password":"secret123"}""";

        var result = HelperFunctions.CheckForProtectedFields(json, ProtectedFields);

        result.ShouldNotBeNull();
        var parsed = JsonNode.Parse(result)!.AsObject();
        parsed["Password"]!.GetValue<string>().ShouldBe("******");
    }

    [Fact]
    public void CheckForProtectedFields_String_NestedObject_MasksDeep()
    {
        var json = """{"user":{"password":"deep-secret","name":"Jane"}}""";

        var result = HelperFunctions.CheckForProtectedFields(json, ProtectedFields);

        result.ShouldNotBeNull();
        var parsed = JsonNode.Parse(result)!.AsObject();
        parsed["user"]!["password"]!.GetValue<string>().ShouldBe("******");
        parsed["user"]!["name"]!.GetValue<string>().ShouldBe("Jane");
    }

    [Fact]
    public void CheckForProtectedFields_String_Array_MasksInArrayElements()
    {
        var json = """{"items":[{"token":"abc"},{"token":"def"}]}""";

        var result = HelperFunctions.CheckForProtectedFields(json, ProtectedFields);

        result.ShouldNotBeNull();
        var parsed = JsonNode.Parse(result)!.AsObject();
        var items = parsed["items"]!.AsArray();
        items[0]!["token"]!.GetValue<string>().ShouldBe("******");
        items[1]!["token"]!.GetValue<string>().ShouldBe("******");
    }

    [Fact]
    public void CheckForProtectedFields_String_EmbeddedJsonString_ParsesAndMasks()
    {
        var innerJson = """{"password":"embedded-secret"}""";
        var outerJson = $$"""{"data":"{{innerJson.Replace("\"", "\\\"")}}"}""";

        var result = HelperFunctions.CheckForProtectedFields(outerJson, ProtectedFields);

        result.ShouldNotBeNull();
        var parsed = JsonNode.Parse(result)!.AsObject();
        // After masking, the embedded JSON string should have been parsed into a JsonObject
        var data = parsed["data"]!.AsObject();
        data["password"]!.GetValue<string>().ShouldBe("******");
    }

    [Fact]
    public void CheckForProtectedFields_String_InvalidJson_ReturnsOriginal()
    {
        var notJson = "this is not json";

        var result = HelperFunctions.CheckForProtectedFields(notJson, ProtectedFields);

        result.ShouldBe(notJson);
    }

    [Fact]
    public void CheckForProtectedFields_String_NonObjectJson_ReturnsOriginal()
    {
        var jsonArray = """[1, 2, 3]""";

        var result = HelperFunctions.CheckForProtectedFields(jsonArray, ProtectedFields);

        result.ShouldBe(jsonArray);
    }

    // --- JsonObject overload ---

    [Fact]
    public void CheckForProtectedFields_JsonObject_NullInput_ReturnsNull()
    {
        JsonObject? obj = null;

        var result = HelperFunctions.CheckForProtectedFields(obj, ProtectedFields);

        result.ShouldBeNull();
    }

    // --- Object overload ---

    [Fact]
    public void CheckForProtectedFields_Object_NullInput_ReturnsNull()
    {
        var result = HelperFunctions.CheckForProtectedFields((object?)null, ProtectedFields);

        result.ShouldBeNull();
    }

    [Fact]
    public void CheckForProtectedFields_EmptyProtectedList_ReturnsOriginal()
    {
        var json = """{"password":"secret"}""";

        var result = HelperFunctions.CheckForProtectedFields(json, []);

        result.ShouldBe(json);
    }
}

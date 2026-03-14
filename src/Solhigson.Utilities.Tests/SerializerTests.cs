using System.Text.Json;
using System.Text.Json.Serialization;
using Shouldly;
using Xunit;

namespace Solhigson.Utilities.Tests;

public class SerializerTests
{
    private record SimpleDto(string Name, int Age);

    private record NestedDto(string Name, AddressDto Address);

    private record AddressDto(string City, string Country);

    private class CircularParent
    {
        public string Name { get; set; } = "Parent";
        public CircularChild? Child { get; set; }
    }

    private class CircularChild
    {
        public string Name { get; set; } = "Child";
        public CircularParent? Parent { get; set; }
    }

    // --- SerializeToJson ---

    [Fact]
    public void SerializeToJson_SimpleObject_ProducesCamelCase()
    {
        var dto = new SimpleDto("Test", 25);

        var json = dto.SerializeToJson();

        json.ShouldNotBeNull();
        json.ShouldContain("\"name\"");
        json.ShouldContain("\"age\"");
        // Verify PascalCase keys are NOT present (exact case-sensitive check)
        json.ShouldNotContain("\"Name\"", Case.Sensitive);
        json.ShouldNotContain("\"Age\"", Case.Sensitive);
    }

    [Fact]
    public void SerializeToJson_NullInput_ReturnsNull()
    {
        object? obj = null;

        var json = obj.SerializeToJson();

        json.ShouldBeNull();
    }

    [Fact]
    public void SerializeToJson_WithIndent_ProducesIndented()
    {
        var dto = new SimpleDto("Test", 25);

        var json = dto.SerializeToJson(indent: true);

        json.ShouldNotBeNull();
        json.ShouldContain("\n");
    }

    [Fact]
    public void SerializeToJson_CustomOptions_UsesCustom()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // PascalCase
        };
        var dto = new SimpleDto("Test", 25);

        var json = dto.SerializeToJson(jsonSerializerOptions: options);

        json.ShouldNotBeNull();
        json.ShouldContain("\"Name\"");
        json.ShouldContain("\"Age\"");
    }

    [Fact]
    public void SerializeToJson_CustomOptions_UsesProvidedOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            WriteIndented = true,
        };
        var dto = new SimpleDto("Test", 25);

        var json = dto.SerializeToJson(jsonSerializerOptions: options);

        json.ShouldNotBeNull();
        json.ShouldContain("\"Name\"");
        json.ShouldContain("\n");
    }

    [Fact]
    public void SerializeToJson_ReferenceLoop_IgnoresCycles()
    {
        var parent = new CircularParent();
        var child = new CircularChild { Parent = parent };
        parent.Child = child;

        var json = parent.SerializeToJson();

        json.ShouldNotBeNull();
        json.ShouldContain("\"name\"");
        json.ShouldContain("\"child\"");
    }

    // --- DeserializeFromJson ---

    [Fact]
    public void DeserializeFromJson_CamelCaseInput_DeserializesCorrectly()
    {
        var json = """{"name":"Test","age":25}""";

        var result = json.DeserializeFromJson<SimpleDto>();

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test");
        result.Age.ShouldBe(25);
    }

    [Fact]
    public void DeserializeFromJson_PascalCaseInput_DeserializesCorrectly()
    {
        var json = """{"Name":"Test","Age":25}""";

        var result = json.DeserializeFromJson<SimpleDto>();

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test");
        result.Age.ShouldBe(25);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DeserializeFromJson_NullOrEmpty_ReturnsDefault(string? input)
    {
        var result = input.DeserializeFromJson<SimpleDto>();

        result.ShouldBeNull();
    }

    // --- SerializeToKeyValue ---

    [Fact]
    public void SerializeToKeyValue_SimpleObject_ProducesPascalCaseKeys()
    {
        var dto = new SimpleDto("Lagos", 5);

        var result = dto.SerializeToKeyValue();

        result.ShouldNotBeNull();
        result.ShouldContainKey("Name");
        result.ShouldContainKey("Age");
        result["Name"].ShouldBe("Lagos");
        result["Age"].ShouldBe("5");
    }

    [Fact]
    public void SerializeToKeyValue_NestedObject_FlattensWithDotPath()
    {
        var dto = new NestedDto("Test", new AddressDto("Lagos", "Nigeria"));

        var result = dto.SerializeToKeyValue();

        result.ShouldNotBeNull();
        result.ShouldContainKey("Name");
        result.ShouldContainKey("Address.City");
        result.ShouldContainKey("Address.Country");
        result["Address.City"].ShouldBe("Lagos");
    }

    [Fact]
    public void SerializeToKeyValue_NullInput_ReturnsNull()
    {
        object? obj = null;

        var result = obj.SerializeToKeyValue();

        result.ShouldBeNull();
    }

    // --- DefaultJsonSerializerOptions (internal) ---

    [Fact]
    public void DefaultJsonSerializerOptions_IsCamelCase()
    {
        Serializer.DefaultJsonSerializerOptions.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
    }

    [Fact]
    public void DefaultJsonSerializerOptions_IsCaseInsensitive()
    {
        Serializer.DefaultJsonSerializerOptions.PropertyNameCaseInsensitive.ShouldBeTrue();
    }

    [Fact]
    public void DefaultJsonSerializerOptions_IgnoresCycles()
    {
        Serializer.DefaultJsonSerializerOptions.ReferenceHandler.ShouldBe(ReferenceHandler.IgnoreCycles);
    }
}

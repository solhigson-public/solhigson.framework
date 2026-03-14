using System.Text.Json;
using Shouldly;
using Solhigson.Framework.Logging;
using Xunit;

namespace Solhigson.Framework.Tests;

public class ExceptionInfoTests
{
    [Fact]
    public void Serialization_UsesPascalCasePropertyNames()
    {
        var info = new ExceptionInfo { Type = "System.Exception", Message = "fail" };

        var json = JsonSerializer.Serialize(info);

        json.ShouldContain("\"Type\"");
        json.ShouldContain("\"Message\"");
    }

    [Fact]
    public void Serialization_AllFieldsPopulated_AllPresent()
    {
        var info = new ExceptionInfo
        {
            Type = "ArgumentException",
            Message = "Invalid arg",
            StackTrace = "at Foo.Bar()",
            Source = "MyAssembly",
        };

        var json = JsonSerializer.Serialize(info);

        json.ShouldContain("\"Type\":\"ArgumentException\"");
        json.ShouldContain("\"Message\":\"Invalid arg\"");
        json.ShouldContain("\"StackTrace\":\"at Foo.Bar()\"");
        json.ShouldContain("\"Source\":\"MyAssembly\"");
    }

    [Fact]
    public void Serialization_NullProperties_IncludedAsNull()
    {
        var info = new ExceptionInfo { Type = "Exception" };

        var json = JsonSerializer.Serialize(info);

        json.ShouldContain("\"Message\":null");
        json.ShouldContain("\"StackTrace\":null");
    }

    [Fact]
    public void Serialization_NestedInnerException_Serializes()
    {
        var info = new ExceptionInfo
        {
            Type = "Outer",
            Message = "outer msg",
            InnerException = new ExceptionInfo
            {
                Type = "Inner",
                Message = "inner msg",
            }
        };

        var json = JsonSerializer.Serialize(info);
        var deserialized = JsonSerializer.Deserialize<ExceptionInfo>(json);

        deserialized.ShouldNotBeNull();
        deserialized.InnerException.ShouldNotBeNull();
        deserialized.InnerException.Type.ShouldBe("Inner");
        deserialized.InnerException.Message.ShouldBe("inner msg");
    }

    [Fact]
    public void Deserialization_PascalCaseInput_MapsToProperties()
    {
        var json = """{"Type":"NullRef","Message":"Object ref","StackTrace":"at X","Source":"Lib","InnerException":null}""";

        var info = JsonSerializer.Deserialize<ExceptionInfo>(json);

        info.ShouldNotBeNull();
        info.Type.ShouldBe("NullRef");
        info.Message.ShouldBe("Object ref");
        info.StackTrace.ShouldBe("at X");
        info.Source.ShouldBe("Lib");
        info.InnerException.ShouldBeNull();
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var a = new ExceptionInfo { Type = "Ex", Message = "msg" };
        var b = new ExceptionInfo { Type = "Ex", Message = "msg" };

        a.ShouldBe(b);
    }
}

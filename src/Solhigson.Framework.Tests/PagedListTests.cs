using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Shouldly;
using Solhigson.Framework.Data;
using Xunit;

namespace Solhigson.Framework.Tests;

public class PagedListTests
{
    [Fact]
    public void GetMetaData_ReturnsCamelCaseJson()
    {
        var list = new PagedList<string>(["a", "b"], count: 10, pageNumber: 1, pageSize: 5);

        var json = list.GetMetaData();
        var node = JsonNode.Parse(json);

        node.ShouldNotBeNull();
        node["totalCount"].ShouldNotBeNull();
        node["pageSize"].ShouldNotBeNull();
        node["currentPage"].ShouldNotBeNull();
        node["totalPages"].ShouldNotBeNull();
        node["hasNext"].ShouldNotBeNull();
        node["hasPrevious"].ShouldNotBeNull();
    }

    [Fact]
    public void GetMetaData_ValuesCorrect()
    {
        var list = new PagedList<int>([1, 2, 3], count: 25, pageNumber: 2, pageSize: 10);

        var json = list.GetMetaData();
        var node = JsonNode.Parse(json)!;

        node["totalCount"]!.GetValue<long>().ShouldBe(25);
        node["pageSize"]!.GetValue<int>().ShouldBe(10);
        node["currentPage"]!.GetValue<int>().ShouldBe(2);
        node["totalPages"]!.GetValue<int>().ShouldBe(3);
        node["hasNext"]!.GetValue<bool>().ShouldBeTrue();
        node["hasPrevious"]!.GetValue<bool>().ShouldBeTrue();
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var list = new PagedList<string>(["x"], count: 50, pageNumber: 3, pageSize: 10);

        list.CurrentPage.ShouldBe(3);
        list.TotalPages.ShouldBe(5);
        list.PageSize.ShouldBe(10);
        list.TotalCount.ShouldBe(50);
        list.Results.Count.ShouldBe(1);
        list.Results[0].ShouldBe("x");
    }

    [Fact]
    public void Constructor_ZeroPageSize_DefaultsTo20()
    {
        // Note: PageSize property defaults to 20 when pageSize param is <= 0,
        // but TotalPages uses the raw parameter (pre-existing behavior).
        var list = new PagedList<string>([], count: 100, pageNumber: 1, pageSize: 0);

        list.PageSize.ShouldBe(20);
    }

    [Fact]
    public void Constructor_ZeroCount_TotalPagesIs1()
    {
        var list = new PagedList<string>([], count: 0, pageNumber: 1, pageSize: 10);

        list.TotalPages.ShouldBe(1);
    }

    [Fact]
    public void HasPrevious_FirstPage_False()
    {
        var list = new PagedList<string>([], count: 50, pageNumber: 1, pageSize: 10);

        list.HasPrevious.ShouldBeFalse();
    }

    [Fact]
    public void HasPrevious_SecondPage_True()
    {
        var list = new PagedList<string>([], count: 50, pageNumber: 2, pageSize: 10);

        list.HasPrevious.ShouldBeTrue();
    }

    [Fact]
    public void HasNext_LastPage_False()
    {
        var list = new PagedList<string>([], count: 50, pageNumber: 5, pageSize: 10);

        list.HasNext.ShouldBeFalse();
    }

    [Fact]
    public void HasNext_NotLastPage_True()
    {
        var list = new PagedList<string>([], count: 50, pageNumber: 3, pageSize: 10);

        list.HasNext.ShouldBeTrue();
    }

    [Fact]
    public void Any_EmptyResults_False()
    {
        var list = new PagedList<string>([], count: 0, pageNumber: 1, pageSize: 10);

        list.Any().ShouldBeFalse();
    }

    [Fact]
    public void Any_WithResults_True()
    {
        var list = new PagedList<string>(["item"], count: 1, pageNumber: 1, pageSize: 10);

        list.Any().ShouldBeTrue();
    }

    [Fact]
    public void StaticCreate_ReturnsPagedList()
    {
        var list = PagedList.Create(new List<int> { 1, 2, 3 }, count: 10, pageNumber: 1, pageSize: 5);

        list.ShouldNotBeNull();
        list.Results.Count.ShouldBe(3);
        list.TotalCount.ShouldBe(10);
    }
}

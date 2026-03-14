using System;
using System.Collections.Generic;
using System.Linq;
using Solhigson.Utilities;

namespace Solhigson.Framework.Data;

public record PagedList
{
    public static PagedList<TK> Create<TK>(List<TK> items, long count, int pageNumber, int pageSize)
    {
        return new PagedList<TK>(items, count, pageNumber, pageSize);
    }
}
public record PagedList<T>
{
    public int CurrentPage { get; }

    public int TotalPages { get; }

    public int PageSize { get;  }

    public long TotalCount { get; }

    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;

    public List<T> Results { get; } = new ();

    public bool Any()
    {
        return Results.Any();
    }

    internal string GetMetaData()
    {
        return new
        {
            TotalCount,
            PageSize,
            CurrentPage,
            TotalPages,
            HasNext,
            HasPrevious
        }.SerializeToJson();
    }

    internal PagedList(IEnumerable<T> items, long count, int pageNumber, int pageSize)
    {
        TotalCount = count;
        PageSize = pageSize <= 0 ? 20 : pageSize;
        CurrentPage = pageNumber;
        TotalPages = count <= 0 ? 1 : (int)Math.Ceiling(count / (double)pageSize);
        Results.AddRange(items);
    }
}

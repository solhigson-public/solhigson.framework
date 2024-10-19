using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

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
    [JsonProperty]
    public int CurrentPage { get; }
        
    [JsonProperty]
    public int TotalPages { get; }
        
    [JsonProperty]
    public int PageSize { get;  }
        
    [JsonProperty]
    public long TotalCount { get; }
        
    [JsonProperty]
    public bool HasPrevious => CurrentPage > 1;
    [JsonProperty]
    public bool HasNext => CurrentPage < TotalPages;

    [JsonProperty]
    public List<T> Results { get; } = new ();

    public bool Any()
    {
        return Results.Any();
    }

    internal string GetMetaData()
    {
        return JsonConvert.SerializeObject(new
        {
            TotalCount,
            PageSize,
            CurrentPage,
            TotalPages,
            HasNext,
            HasPrevious
        });
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
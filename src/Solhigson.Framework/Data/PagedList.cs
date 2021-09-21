using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Solhigson.Framework.Data
{
    public class PagedList<T>
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

        public PagedList(List<T> items, long count, int pageNumber, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            Results.AddRange(items);
        }
    }
}
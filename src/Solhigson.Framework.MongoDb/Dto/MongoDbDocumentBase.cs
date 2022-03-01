using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Solhigson.Framework.MongoDb.Dto;

public record MongoDbDocumentBase
{
    public MongoDbDocumentBase()
    {
            
    }
    public string Id { get; set; }
        
    [JsonPropertyName("_ttl")]
    [JsonProperty("_ttl")]
    public DateTime Ttl { get; set; }
}
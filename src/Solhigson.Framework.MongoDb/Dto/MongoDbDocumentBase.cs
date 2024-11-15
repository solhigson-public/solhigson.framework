using System;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using Newtonsoft.Json;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.MongoDb.Dto;

public record MongoDbDocumentBase : IMongoDbDocumentBase
{
    public string? Id { get; set; }
        
    [JsonPropertyName("_ttl")]
    [JsonProperty("_ttl")]
    public DateTime Ttl { get; set; }
    
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public double TimeToLive
    {
        get => Ttl.ToUnixTimestamp();
        set => Ttl = value.FromUnixTimestamp();
    }

}
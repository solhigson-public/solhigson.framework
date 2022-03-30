using System;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace Solhigson.Framework.MongoDb.Dto;

public record MongoDbDocumentBase : IMongoDbDocumentBase
{
    public MongoDbDocumentBase()
    {
            
    }
    public string Id { get; set; }
        
    [JsonPropertyName("_ttl")]
    [JsonProperty("_ttl")]
    public DateTime Ttl { get; set; }
}
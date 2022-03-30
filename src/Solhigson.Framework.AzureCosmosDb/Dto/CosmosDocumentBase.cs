using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Solhigson.Framework.AzureCosmosDb.Dto;

public record CosmosDocumentBase : ICosmosDocumentBase
{
    public virtual string PartitionKey => throw new System.NotImplementedException();

    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; }
        
    [JsonProperty("_ts")]
    [JsonPropertyName("_ts")]
    public double Timestamp { get; set; }

    [JsonProperty("ttl")]
    [JsonPropertyName("ttl")]
    public int? TimeToLive { get; set; }    
}
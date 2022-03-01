using System;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.MongoDb.Dto;

public record MongoDbLog : MongoDbDocumentBase
{
    [JsonProperty("Source")]
    [JsonPropertyName("Source")]
    public string Source { get; set; }

    [JsonPropertyName("LogLevel")]
    [JsonProperty("LogLevel")]
    public string LogLevel { get; set; }

    [JsonPropertyName("Description")]
    [JsonProperty("Description")]
    public string Description { get; set; }

    [JsonPropertyName("Group")]
    [JsonProperty("Group")]
    public string Group { get; set; }

    [JsonPropertyName("Exception")]
    [JsonProperty("Exception")]
    public string Exception { get; set; }

    [JsonPropertyName("Data")]
    [JsonProperty("Data")]
    public string Data { get; set; }

    [JsonPropertyName("User")]
    [JsonProperty("User")]
    public string User { get; set; }

    [JsonPropertyName("ServiceName")]
    [JsonProperty("ServiceName")]
    public string ServiceName { get; set; }

    [JsonPropertyName("ServiceType")]
    [JsonProperty("ServiceType")]
    public string ServiceType { get; set; }

    [JsonPropertyName("ServiceUrl")]
    [JsonProperty("ServiceUrl")]
    public string ServiceUrl { get; set; }

    [JsonPropertyName("Status")]
    [JsonProperty("Status")]
    public string Status { get; set; }

    [JsonPropertyName("ChainId")]
    [JsonProperty("ChainId")]
    public string ChainId { get; set; }
        
    [JsonPropertyName("Timestamp")]
    [JsonProperty("Timestamp")]
    public double Timestamp { get; set; }


    [Newtonsoft.Json.JsonIgnore]
    public DateTime Date
    {
        get
        {
            try
            {
                return DateUtils.FromUnixTimestamp(Timestamp);
            }
            catch (Exception e)
            {
                this.ELogError(e);
            }

            return DateTime.UtcNow;
        }
    }
}
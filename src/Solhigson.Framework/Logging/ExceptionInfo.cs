using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Solhigson.Framework.Logging;

public record ExceptionInfo
{
    [JsonProperty("Type")]
    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    [JsonProperty("Message")]
    [JsonPropertyName("Message")]
    public string? Message { get; set; }
    
    [JsonProperty("StackTrace")]
    [JsonPropertyName("StackTrace")]
    public string? StackTrace { get; set; }
    
    [JsonProperty("Source")]
    [JsonPropertyName("Source")]
    public string? Source { get; set; }

    [JsonProperty("InnerException")]
    [JsonPropertyName("InnerException")]
    public ExceptionInfo? InnerException { get; set; }
}
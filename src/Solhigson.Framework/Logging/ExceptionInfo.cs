using System;
using System.Text.Json.Serialization;

namespace Solhigson.Framework.Logging;

public record ExceptionInfo
{
    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    [JsonPropertyName("Message")]
    public string? Message { get; set; }

    [JsonPropertyName("StackTrace")]
    public string? StackTrace { get; set; }

    [JsonPropertyName("Source")]
    public string? Source { get; set; }

    [JsonPropertyName("InnerException")]
    public ExceptionInfo? InnerException { get; set; }
}

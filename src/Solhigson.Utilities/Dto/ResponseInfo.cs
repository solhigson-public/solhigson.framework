using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Solhigson.Utilities.Dto;

public struct ResponseInfo
{
    internal const string DefaultMessage = "An unexpected error has occurred.";
    private string _statusCode = Solhigson.Utilities.StatusCode.UnExpectedError;
    private string? _message = DefaultMessage;
    private bool _initialized;

    [Newtonsoft.Json.JsonIgnore] 
    [System.Text.Json.Serialization.JsonIgnore]
    public object? ErrorData { get; set; }

    [Newtonsoft.Json.JsonIgnore] 
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsSuccessful => StatusCode == Solhigson.Utilities.StatusCode.Successful;

    [JsonProperty("statusCode")]
    [JsonPropertyName("statusCode")]
    public string StatusCode
    {
        get => _initialized ? _statusCode : Solhigson.Utilities.StatusCode.UnExpectedError;
        set
        {
            _statusCode = value;
            _initialized = true;
        }
    }

    [JsonProperty("message")]
    [JsonPropertyName("message")]
    public string? Message
    {
        get => _initialized ? _message : DefaultMessage;
        set
        {
            _message = value;
            _initialized = true;
        }
    }
    
    //for consistency in resulting json string with ResponseInfo<T> - will always be null
    [JsonProperty("data")] 
    [JsonPropertyName("data")]
    public object? Data { get; private set; }

    public T? GetError<T>()
    {
        return ErrorData is T error ? error : default;
    }

    public static ResponseInfo SuccessResult(string? message = "")
    {
        return new ResponseInfo().Success(message);
    }

    public static ResponseInfo<T> SuccessResult<T>(T result, string? message = "")
    {
        return new ResponseInfo<T>().Success(result, message);
    }

    public static ResponseInfo FailedResult(string? message = DefaultMessage,
        string responseCode = Solhigson.Utilities.StatusCode.UnExpectedError, object? errorData = null)
    {
        return new ResponseInfo().Fail(message, responseCode, errorData);
    }

    public static ResponseInfo<T> FailedResult<T>(string? message = DefaultMessage,
        string responseCode = Solhigson.Utilities.StatusCode.UnExpectedError, object? errorData = null,
        T? result = default)
    {
        return new ResponseInfo<T>().Fail(message, responseCode, errorData, result);
    }

    public ResponseInfo Success(string? message = "")
    {
        StatusCode = Solhigson.Utilities.StatusCode.Successful;
        Message = message;
        _initialized = true;
        return this;
    }

    public ResponseInfo Fail(string? message = DefaultMessage,
        string responseCode = Solhigson.Utilities.StatusCode.UnExpectedError, object? errorData = null)
    {
        Message = message;
        StatusCode = responseCode;
        ErrorData = errorData;
        _initialized = true;
        return this;
    }
        
    public ResponseInfo(string? message = DefaultMessage, 
        string statusCode = Solhigson.Utilities.StatusCode.UnExpectedError)
    {
        _message = message;
        _statusCode = statusCode;
        ErrorData = null;
        Data = null;
        _initialized = true;
    }
}

public struct ResponseInfo<T>(
    string? message = ResponseInfo.DefaultMessage,
    string statusCode = StatusCode.UnExpectedError,
    T? result = default)
{
    private ResponseInfo _responseInfo = new(message, statusCode);

    [JsonProperty("statusCode")] 
    [JsonPropertyName("statusCode")]
    public string StatusCode
    {
        get => _responseInfo.StatusCode;
        set => _responseInfo.StatusCode = value;
    }

    [JsonProperty("message")] 
    [JsonPropertyName("message")]
    public string? Message
    {
        get => _responseInfo.Message;
        set => _responseInfo.Message = value;
    }

    [JsonProperty("data")] 
    [JsonPropertyName("data")]
    public T? Data { get; private set; } = result;

    [Newtonsoft.Json.JsonIgnore] 
    [System.Text.Json.Serialization.JsonIgnore]
    [MemberNotNullWhen(true, nameof(Data))]
    public bool IsSuccessful => _responseInfo.IsSuccessful;

    public ResponseInfo<T> Success(T result, string? message = "")
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result),
                $"result cannot be null when calling ResponseInfo<>.Success({typeof(T).FullName}, string)");
        }

        _responseInfo.Success(message);
        Data = result;
        return this;
    }

    public ResponseInfo<T> Fail(string? message = ResponseInfo.DefaultMessage,
        string responseCode = Solhigson.Utilities.StatusCode.UnExpectedError, object? errorData = null,
        T? result = default)
    {
        Data = result;
        _responseInfo.Fail(message, responseCode, errorData);
        return this;
    }
        
    public ResponseInfo<T> Fail(ResponseInfo response, T? result = default)
    {
        Data = result;
        _responseInfo = response;
        return this;
    }

    [Newtonsoft.Json.JsonIgnore] 
    [System.Text.Json.Serialization.JsonIgnore]
    public ResponseInfo InfoResult => _responseInfo;
        
    public Tk? GetError<Tk>()
    {
        return _responseInfo.GetError<Tk>();
    }
        
    public ResponseInfo<T> SetStatusCode(string statusCode)
    {
        _responseInfo.StatusCode = statusCode;
        return this;
    }
        
    [Newtonsoft.Json.JsonIgnore] 
    [System.Text.Json.Serialization.JsonIgnore]
    public object? ErrorData
    {
        get => _responseInfo.ErrorData;
        set => _responseInfo.ErrorData = value;
    }




}
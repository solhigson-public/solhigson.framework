using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Solhigson.Framework.Infrastructure
{
    public struct ResponseInfo
    {
        [Newtonsoft.Json.JsonIgnore] 
        [System.Text.Json.Serialization.JsonIgnore]
        public object ErrorData { get; set; }

        [Newtonsoft.Json.JsonIgnore] 
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsSuccessful => StatusCode == Infrastructure.StatusCode.Successful;

        [JsonProperty("statusCode")] 
        [JsonPropertyName("statusCode")]
        public string StatusCode { get; set; }

        [JsonProperty("message")] 
        [JsonPropertyName("message")]
        public string Message { get; set; }

        public ResponseInfo SetResponseCode(string responseCode)
        {
            StatusCode = responseCode;
            return this;
        }

        public T GetError<T>()
        {
            return ErrorData is T error ? error : default;
        }

        public ResponseInfo Success(string message = "")
        {
            StatusCode = Infrastructure.StatusCode.Successful;
            Message = message;
            return this;
        }

        public static ResponseInfo SuccessResult(string message = null)
        {
            return new ResponseInfo().Success(message);
        }

        public static ResponseInfo<T> SuccessResult<T>(T result, string message = null)
        {
            return new ResponseInfo<T>().Success(result, message);
        }

        public static ResponseInfo FailedResult(string message = "An unexpected error has occurred.",
            string responseCode = Infrastructure.StatusCode.UnExpectedError, object errorData = null)
        {
            return new ResponseInfo().Fail(message, responseCode, errorData);
        }

        public static ResponseInfo<T> FailedResult<T>(string message = "An unexpected error has occurred.",
            string responseCode = Infrastructure.StatusCode.UnExpectedError, object errorData = null,
            T result = default)
        {
            return new ResponseInfo<T>().Fail(message, responseCode, errorData, result);
        }

        public ResponseInfo Fail(string message = "An unexpected error has occurred.",
            string responseCode = Infrastructure.StatusCode.UnExpectedError, object errorData = null)
        {
            Message = message;
            StatusCode = responseCode;
            ErrorData = errorData;
            return this;
        }
        
        public ResponseInfo(string message = "An unexpected error has occurred.", 
            string statusCode = Infrastructure.StatusCode.UnExpectedError)
        {
            Message = message;
            StatusCode = statusCode;
            ErrorData = null;
        }
    }

    public struct ResponseInfo<T>
    {
        private ResponseInfo _responseInfo;

        public ResponseInfo(string message = "An unexpected error has occurred.",
            string statusCode = Infrastructure.StatusCode.UnExpectedError,
            T result = default)
        {
            Data = result;
            _responseInfo = new ResponseInfo(message, statusCode);
        }
        
        [JsonProperty("statusCode")] 
        [JsonPropertyName("statusCode")]
        public string StatusCode
        {
            get => _responseInfo.StatusCode;
            set => _responseInfo.StatusCode = value;
        }

        [JsonProperty("message")] 
        [JsonPropertyName("message")]
        public string Message
        {
            get => _responseInfo.Message;
            set => _responseInfo.Message = value;
        }

        [JsonProperty("data")] 
        [JsonPropertyName("data")]
        public T Data { get; private set; }

        [Newtonsoft.Json.JsonIgnore] 
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsSuccessful => _responseInfo.IsSuccessful;

        public ResponseInfo<T> Success(T result, string message = null)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result),
                    $"result cannot be null when calling ResponseInfo<>.Success({typeof(T).FullName}, string)");
            }

            _responseInfo.Success(message);
            Data = result;
            return this;
        }

        public ResponseInfo<T> Fail(string message = "An unexpected error has occurred.",
            string responseCode = Infrastructure.StatusCode.UnExpectedError, object errorData = null,
            T result = default)
        {
            Data = result;
            _responseInfo.Fail(message, responseCode, errorData);
            return this;
        }
        
        public ResponseInfo<T> Fail(ResponseInfo response, T result = default)
        {
            Data = result;
            _responseInfo = response;
            return this;
        }

        [Newtonsoft.Json.JsonIgnore] 
        [System.Text.Json.Serialization.JsonIgnore]
        public ResponseInfo ResponseInfoResult => _responseInfo;

    }
}
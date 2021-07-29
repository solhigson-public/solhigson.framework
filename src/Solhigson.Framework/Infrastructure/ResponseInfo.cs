using System;
using Newtonsoft.Json;

namespace Solhigson.Framework.Infrastructure
{
    public class ResponseInfo
    {
        [JsonIgnore] public object ErrorData { get; set; }

        [JsonIgnore] public bool IsSuccessful => StatusCode == Infrastructure.StatusCode.Successful;

        [JsonProperty("statusCode")] public string StatusCode { get; set; }

        [JsonProperty("message")] public string Message { get; set; }

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

        public static ResponseInfo SuccessResult(string message = "")
        {
            return new ResponseInfo().Success(message);
        }

        public static ResponseInfo<T> SuccessResult<T>(T result, string message = "")
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
    }

    public class ResponseInfo<T> : ResponseInfo
    {
        [JsonProperty("data")] public T Data { get; private set; }

        public ResponseInfo<T> Success(T result, string message = "")
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result),
                    $"result cannot be null when calling ResponseInfo<>.Success({typeof(T).FullName}, string)");
            }

            base.Success(message);
            Data = result;
            return this;
        }

        public ResponseInfo<T> Fail(string message = "An unexpected error has occurred.",
            string responseCode = Infrastructure.StatusCode.UnExpectedError, object errorData = null,
            T result = default)
        {
            Data = result;
            base.Fail(message, responseCode, errorData);
            return this;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Web.Api
{
    public sealed class ApiRequestService : IApiRequestService
    {
        internal const string DefaultNamedHttpClient = "ApiRequestService";
        public const string ContentTypePlain = "text/plain";
        public const string ContentTypeJson = "application/json";
        public const string ContentTypeXml = "application/xml";
        public const string ContentTypeXWwwFormUrlencoded = "application/x-www-form-urlencoded";
        private readonly LogWrapper _logger = new LogWrapper("ApiRequestHelper");
        private readonly IHttpClientFactory _httpClientFactory;

        public ApiRequestService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        #region GET Requests

        public async Task<ApiRequestResponse> GetDataJsonAsync(string uri,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0)
        {
            return await SendRequestAsync(uri, HttpMethod.Get, timeOut: timeOut, headers: headers,
                serviceName: serviceName, serviceType: serviceType, serviceDescription: serviceDescription);
        }

        public async Task<ApiRequestResponse> GetDataXmlAsync(string uri,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0)
        {
            return await SendRequestAsync(uri, HttpMethod.Get, format: ContentTypeXml, timeOut: timeOut,
                headers: headers,
                serviceName: serviceName, serviceType: serviceType, serviceDescription: serviceDescription);
        }

        public async Task<ApiRequestResponse> GetDataPlainAsync(string uri,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0)
        {
            return await SendRequestAsync(uri, HttpMethod.Get, format: ContentTypePlain, timeOut: timeOut,
                headers: headers,
                serviceName: serviceName, serviceType: serviceType, serviceDescription: serviceDescription);
        }

        public async Task<ApiRequestResponse<T>> GetDataJsonAsync<T>(string uri,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0) where T : class
        {
            return await SendRequestAsync<T>(uri, HttpMethod.Get, timeOut: timeOut, headers: headers,
                serviceName: serviceName, serviceType: serviceType, serviceDescription: serviceDescription);
        }

        public async Task<ApiRequestResponse<T>> GetDataXmlAsync<T>(string uri,
            Dictionary<string, string> headers = null,
            string serviceName = null, string serviceDescription = null, string serviceType = null, int timeOut = 0)
            where T : class
        {
            return await SendRequestAsync<T>(uri, HttpMethod.Get, format: ContentTypeXml, timeOut: timeOut,
                headers: headers,
                serviceName: serviceName, serviceType: serviceType, serviceDescription: serviceDescription);
        }

        #endregion

        #region POST Requests

        public async Task<ApiRequestResponse> PostDataJsonAsync(string uri, string data,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0)
        {
            return await SendRequestAsync(uri, HttpMethod.Post, data, timeOut: timeOut, headers: headers,
                serviceName: serviceName, serviceType: serviceType, serviceDescription: serviceDescription);
        }

        public async Task<ApiRequestResponse> PostDataXmlAsync(string uri, string data,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0)
        {
            return await SendRequestAsync(uri, HttpMethod.Post, data, ContentTypeXml, headers, serviceName,
                serviceDescription, serviceType,
                timeOut);
        }

        public async Task<ApiRequestResponse> PostDataAsync(string uri, string data, string contentType,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0)
        {
            return await SendRequestAsync(uri, HttpMethod.Post, data, contentType, headers, serviceName,
                serviceDescription, serviceType,
                timeOut);
        }

        public async Task<ApiRequestResponse<T>> PostDataJsonAsync<T>(string uri, string data,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0) where T : class
        {
            return await SendRequestAsync<T>(uri, HttpMethod.Post, data, timeOut: timeOut, headers: headers,
                serviceName: serviceName, serviceType: serviceType, serviceDescription: serviceDescription);
        }

        public async Task<ApiRequestResponse<T>> PostDataXWwwFormUrlencodedAsync<T>(string uri,
            string data,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0) where T : class
        {
            return await SendRequestAsync<T>(uri, HttpMethod.Post, data, ContentTypeXWwwFormUrlencoded, headers,
                serviceName, serviceDescription, serviceType, timeOut);
        }

        public async Task<ApiRequestResponse<T>> PostDataXWwwFormUrlencodedAsync<T>(string uri,
            IDictionary<string, string> data,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0) where T : class
        {
            return await PostDataXWwwFormUrlencodedAsync<T>(uri,
                await new FormUrlEncodedContent(data).ReadAsStringAsync(),
                headers, serviceName, serviceDescription, serviceType, timeOut);
        }

        public async Task<ApiRequestResponse> PostDataXWwwFormUrlencodedAsync(string uri,
            IDictionary<string, string> data,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0)
        {
            return await PostDataXWwwFormUrlencodedAsync(uri, await new FormUrlEncodedContent(data).ReadAsStringAsync(),
                headers, serviceName, serviceDescription, serviceType, timeOut);
        }


        public async Task<ApiRequestResponse> PostDataXWwwFormUrlencodedAsync(string uri, string data,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0)
        {
            return await SendRequestAsync(uri, HttpMethod.Post, data, ContentTypeXWwwFormUrlencoded, headers,
                serviceName, serviceDescription, serviceType, timeOut);
        }


        public async Task<ApiRequestResponse<T>> PostDataXmlAsync<T>(string uri, string data,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0) where T : class
        {
            return await SendRequestAsync<T>(uri, HttpMethod.Post, data, ContentTypeXml, headers, serviceName,
                serviceDescription,
                serviceType, timeOut);
        }

        #endregion

        #region Helpers

        private async Task<ApiRequestResponse> SendRequestAsync(string uri, HttpMethod method,
            string data = "", string format = ContentTypeJson,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0)
        {
            return await SendRequestAsync<object>(uri, method, data, format, headers, serviceName, serviceDescription,
                serviceType,
                timeOut);
        }

        private async Task<ApiRequestResponse<T>> SendRequestAsync<T>(string uri, HttpMethod method,
            string data = "", string format = ContentTypeJson,
            Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
            string serviceType = null,
            int timeOut = 0) where T : class
        {
            try
            {
                var apiRequestDetails = new ApiRequestDetails(new Uri(uri), method, data)
                {
                    Format = format,
                    Headers = headers,
                    TimeOut = timeOut,
                    ServiceName = serviceName,
                    ServiceType = serviceType,
                    ServiceDescription = serviceDescription
                };
                return await SendRequestAsync<T>(apiRequestDetails);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }

            return new ApiRequestResponse<T>();
        }


        public async Task<ApiRequestResponse> SendRequestAsync(ApiRequestDetails apiRequestDetails)
        {
            return await SendRequestAsync<object>(apiRequestDetails);
        }

        public async Task<ApiRequestResponse<T>> SendRequestAsync<T>(ApiRequestDetails apiRequestDetails)
            where T : class
        {
            return await SendRequestInternalAsync<T>(apiRequestDetails);
        }

        private async Task<ApiRequestResponse<T>> SendRequestInternalAsync<T>(
            ApiRequestDetails apiRequestDetails)
            where T : class
        {
            var method = apiRequestDetails.HttpMethod;
            var data = apiRequestDetails.Payload;
            var format = apiRequestDetails.Format;
            var timeOut = apiRequestDetails.TimeOut;
            var url = apiRequestDetails.Uri.ToString();

            var serviceName = apiRequestDetails.ServiceName;
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                serviceName = apiRequestDetails.Uri.Host;
            }

            var serviceType = apiRequestDetails.ServiceType;
            if (string.IsNullOrWhiteSpace(serviceType))
            {
                serviceType = Constants.ServiceType.External;
            }

            var apiRequestHelperResponse = new ApiRequestResponse<T>();
            var client = _httpClientFactory.CreateClient(DefaultNamedHttpClient);
            client.DefaultRequestHeaders.ExpectContinue = apiRequestDetails.ExpectContinue;
            var request = new HttpRequestMessage();
            //HttpResponseMessage httpResponseMsg = null;
            var traceData = new ApiTraceData
            {
                RequestTime = DateTime.UtcNow,
                Url = url,
                Method = method.ToString(),
                Caller = Constants.ServiceType.Self,
                RequestHeaders = HelperFunctions.ToJsonObject(apiRequestDetails.Headers),
            };
            try
            {
                request.Method = method;
                request.RequestUri = apiRequestDetails.Uri;
                if (!string.IsNullOrWhiteSpace(data))
                {
                    request.Content = new StringContent(data, Encoding.UTF8, format);
                }
                else if (method == HttpMethod.Delete || method == HttpMethod.Put)
                {
                    request.Content ??= new StringContent("", Encoding.UTF8, format);
                }

                if (timeOut > 0)
                {
                    client.Timeout = TimeSpan.FromMilliseconds(timeOut);
                }

                if (apiRequestDetails.Headers != null && apiRequestDetails.Headers.Count > 0)
                {
                    foreach (var key in apiRequestDetails.Headers.Keys)
                    {
                        if (apiRequestDetails.Headers.TryGetValue(key, out var value) &&
                            !string.IsNullOrWhiteSpace(value))
                        {
                            request.Headers.TryAddWithoutValidation(key, value);
                        }
                    }
                }

                apiRequestHelperResponse.Request = request.Content != null
                    ? await request.Content.ReadAsStringAsync()
                    : data;

                /*
                traceData.RequestMessage =
                    HelperFunctions.CheckForProtectedFields(apiRequestHelperResponse.Request, _servicesWrapper);
                */
                traceData.RequestMessage = apiRequestHelperResponse.Request;

                apiRequestHelperResponse.ResponseHeaders = new Dictionary<string, string>();

                apiRequestHelperResponse.HttpResponseMessage = await client.SendAsync(request);
                apiRequestHelperResponse.Response =
                    await apiRequestHelperResponse.HttpResponseMessage.Content.ReadAsStringAsync();

            }
            catch (WebException we)
            {
                apiRequestHelperResponse.Response = we.Message;
                var hResponse = (HttpWebResponse) we.Response;
                if (hResponse == null)
                {
                    _logger.Error(we, $"While sending request to url: {url}");
                    GetStatusCode(we, apiRequestHelperResponse);

                    apiRequestHelperResponse.HttpStatusCode = HttpStatusCode.InternalServerError;
                    apiRequestHelperResponse.HttpStatusDescription = we.Message;
                }
                else
                {
                    await using var stream = hResponse.GetResponseStream();
                    if (stream != null)
                    {
                        using var sReader = new StreamReader(stream);
                        apiRequestHelperResponse.Response = await sReader.ReadToEndAsync();
                    }

                    if (hResponse.Headers != null)
                    {
                        foreach (var header in hResponse.Headers.AllKeys)
                        {
                            apiRequestHelperResponse.ResponseHeaders.Add(header, hResponse.Headers[header]);
                        }
                    }

                    apiRequestHelperResponse.HttpStatusCode = hResponse.StatusCode;
                    apiRequestHelperResponse.HttpStatusDescription = hResponse.StatusDescription;
                }
            }
            catch (Exception e)
            {
                apiRequestHelperResponse.Response = e.Message;
                _logger.Error(e, $"While sending request to url: {url}");
                apiRequestHelperResponse.HttpStatusCode = HttpStatusCode.InternalServerError;
                if ((DateTime.UtcNow - traceData.RequestTime).TotalMilliseconds >= apiRequestDetails.TimeOut)
                {
                    apiRequestHelperResponse.HttpStatusCode = HttpStatusCode.RequestTimeout;
                }

                apiRequestHelperResponse.HttpStatusDescription = e.Message;
            }
            finally
            {
                try
                {
                    traceData.ResponseTime = DateTime.UtcNow;
                    traceData.TimeTaken = HelperFunctions.Format(traceData.ResponseTime - traceData.RequestTime);
                    /*
                    traceData.ResponseMessage =
                        HelperFunctions.CheckForProtectedFields(apiRequestHelperResponse.Response, _servicesWrapper);
                    */
                    traceData.ResponseMessage = apiRequestHelperResponse.Response;
                    var responseFormat = format;
                    if (apiRequestHelperResponse.HttpResponseMessage != null)
                    {
                        apiRequestHelperResponse.HttpStatusCode =
                            apiRequestHelperResponse.HttpResponseMessage.StatusCode;
                        traceData.ResponseHeaders =
                            HelperFunctions.ToJsonObject(apiRequestHelperResponse.HttpResponseMessage.Headers);
                        if (traceData.ResponseHeaders.ContainsKey("Content-Type"))
                        {
                            responseFormat = traceData.ResponseHeaders["Content-Type"].ToString();
                        }
                        if (apiRequestHelperResponse.HttpResponseMessage.Headers != null)
                        {
                            foreach (var (key, value) in apiRequestHelperResponse.HttpResponseMessage.Headers)
                            {
                                apiRequestHelperResponse.ResponseHeaders.Add(key, value.ToString());
                            }
                        }
                    }

                    traceData.StatusCode = ((int) apiRequestHelperResponse.HttpStatusCode).ToString();
                    traceData.StatusCodeDescription = apiRequestHelperResponse.HttpStatusCode.ToString();

                    ExtractObject(apiRequestHelperResponse, responseFormat);
                    var status = HelperFunctions.IsServiceUp(apiRequestHelperResponse.HttpStatusCode)
                        ? Constants.ServiceStatus.Up
                        : Constants.ServiceStatus.Down;

                    var desc = string.IsNullOrWhiteSpace(apiRequestDetails.ServiceDescription)
                        ? "Outbound"
                        : apiRequestDetails.ServiceDescription;

                    _logger.Log(desc, LogLevel.Info, traceData, null,
                        serviceName, serviceType,
                        Constants.Group.ServiceStatus, status, traceData.Url,
                        traceData.GetUserIdentity());
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
            }

            return apiRequestHelperResponse;
        }

        private void GetStatusCode(WebException we, ApiRequestResponse apiRequestResponse)
        {
            if (we.Message.ToLower().Contains("timed out")
                || we.Status == WebExceptionStatus.Timeout
                || we.Status == WebExceptionStatus.Pending)
            {
                apiRequestResponse.HttpStatusCode = HttpStatusCode.RequestTimeout;
            }
        }

        private void ExtractObject<T>(ApiRequestResponse<T> apiRequestResponse, string format)
            where T : class
        {
            try
            {
                if (typeof(T).Name == nameof(Object))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(apiRequestResponse.Response))
                {
                    return;
                }

                var content = apiRequestResponse.Response.Trim();

                if (HelperFunctions.IsValidJson(content))
                {
                    apiRequestResponse.Result = content.DeserializeFromJson<T>();
                }
                else if (HelperFunctions.IsValidXml(content))
                {
                    apiRequestResponse.Result = content.DeserializeFromXml<T>();
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, $"While deserializing response: " +
                                $"{apiRequestResponse.Response} from: {format}");
            }
        }

        #endregion
    }
}
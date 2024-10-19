using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solhigson.Framework.Web.Api;

public interface IApiRequestService
{
    void UseConfiguration(Action<ApiConfiguration> configuration);
    
    Task<ApiRequestResponse> GetDataJsonAsync(string uri,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null);

    Task<ApiRequestResponse<T>> GetDataJsonAsync<T>(string uri,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null) ;

    Task<ApiRequestResponse> GetDataXmlAsync(string uri,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null);

    Task<ApiRequestResponse<T>> GetDataXmlAsync<T>(string uri,
        Dictionary<string, string> headers = null,
        string serviceName = null, string serviceDescription = null, 
        string serviceType = null,  string namedHttpClient = null, int timeOut = 0, bool? logTrace = null)
        ;

    Task<ApiRequestResponse> GetDataPlainAsync(string uri,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null);

    Task<ApiRequestResponse> PostDataJsonAsync(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null);

    Task<ApiRequestResponse<T>> PostDataJsonAsync<T>(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null) ;

    Task<ApiRequestResponse> PostDataXmlAsync(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null);

    Task<ApiRequestResponse<T>> PostDataXmlAsync<T>(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null) ;

    Task<ApiRequestResponse> PostDataAsync(string uri, string data, string contentType,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null);

    Task<ApiRequestResponse<T>> PostDataXWwwFormUrlencodedAsync<T>(string uri,
        string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null) ;

    Task<ApiRequestResponse<T>> PostDataXWwwFormUrlencodedAsync<T>(string uri,
        IDictionary<string, string> data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null) ;

    Task<ApiRequestResponse> PostDataXWwwFormUrlencodedAsync(string uri,
        IDictionary<string, string> data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null);

    Task<ApiRequestResponse> PostDataXWwwFormUrlencodedAsync(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0, bool? logTrace = null);

    Task<ApiRequestResponse> SendRequestAsync(ApiRequestDetails apiRequestDetails);

    Task<ApiRequestResponse<T>> SendRequestAsync<T>(ApiRequestDetails apiRequestDetails)
        ;
}
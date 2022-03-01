using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solhigson.Framework.Web.Api;

public interface IApiRequestService
{
    Task<ApiRequestResponse> GetDataJsonAsync(string uri,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0);

    Task<ApiRequestResponse<T>> GetDataJsonAsync<T>(string uri,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0) where T : class;

    Task<ApiRequestResponse> GetDataXmlAsync(string uri,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0);

    Task<ApiRequestResponse<T>> GetDataXmlAsync<T>(string uri,
        Dictionary<string, string> headers = null,
        string serviceName = null, string serviceDescription = null, 
        string serviceType = null,  string namedHttpClient = null, int timeOut = 0)
        where T : class;

    Task<ApiRequestResponse> GetDataPlainAsync(string uri,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0);

    Task<ApiRequestResponse> PostDataJsonAsync(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0);

    Task<ApiRequestResponse<T>> PostDataJsonAsync<T>(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0) where T : class;

    Task<ApiRequestResponse> PostDataXmlAsync(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0);

    Task<ApiRequestResponse<T>> PostDataXmlAsync<T>(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0) where T : class;

    Task<ApiRequestResponse> PostDataAsync(string uri, string data, string contentType,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0);

    Task<ApiRequestResponse<T>> PostDataXWwwFormUrlencodedAsync<T>(string uri,
        string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0) where T : class;

    Task<ApiRequestResponse<T>> PostDataXWwwFormUrlencodedAsync<T>(string uri,
        IDictionary<string, string> data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0) where T : class;

    Task<ApiRequestResponse> PostDataXWwwFormUrlencodedAsync(string uri,
        IDictionary<string, string> data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0);

    Task<ApiRequestResponse> PostDataXWwwFormUrlencodedAsync(string uri, string data,
        Dictionary<string, string> headers = null, string serviceName = null, string serviceDescription = null,
        string serviceType = null, string namedHttpClient = null,
        int timeOut = 0);

    Task<ApiRequestResponse> SendRequestAsync(ApiRequestDetails apiRequestDetails);

    Task<ApiRequestResponse<T>> SendRequestAsync<T>(ApiRequestDetails apiRequestDetails)
        where T : class;
}
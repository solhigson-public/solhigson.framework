using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using NLog.Common;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Services;

public class AzureLogAnalyticsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    internal const string AzureLogAnalyticsNamedHttpClient = "AzureLogAnalyticsService";

    private string _logName;
    private string _sharedKey;
    private string _workspaceId;

    public AzureLogAnalyticsService(string workspaceId, string sharedKey, string logName, IHttpClientFactory httpClientFactory)
    {
        _workspaceId = workspaceId;
        _sharedKey = sharedKey;
        _logName = logName;
        _httpClientFactory = httpClientFactory;
    }

    private bool RequiredParametersValid()
    {
        if (!string.IsNullOrWhiteSpace(_logName) && !string.IsNullOrWhiteSpace(_sharedKey) &&
            !string.IsNullOrWhiteSpace(_workspaceId))
        {
            return true;
        }

        InternalLogger.Error("One or more parameters for Azure Log Analytics Service is missing. " +
                             "[LogName or WorkspaceId or SharedKey]");
        return false;
    }

    internal bool PostLog(string logInfo)
    {
        if (string.IsNullOrWhiteSpace(logInfo)) return true;

        if (!RequiredParametersValid())
        {
            InternalLogger.Warn("Post to Azure Log Analytics was not successful");
            return true;
        }

        var stringDate = DateTime.UtcNow.ToString("r");
        var hashedSignatureKey =
            GetSignature("POST", logInfo.Length, "application/json", stringDate, "/api/logs");
        var signature = "SharedKey " + _workspaceId + ":" + hashedSignatureKey;

        return PostData(signature, stringDate, logInfo);
    }

    private bool PostData(string signature, string date, string json)
    {
        try
        {
            var url = "https://" + _workspaceId + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";
            var client = _httpClientFactory.CreateClient(AzureLogAnalyticsNamedHttpClient);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Log-Type", _logName);
            client.DefaultRequestHeaders.Add("Authorization", signature);
            client.DefaultRequestHeaders.Add("x-ms-date", date);
            //client.DefaultRequestHeaders.Add("time-generated-field", TimeStampField);

            HttpContent httpContent = new StringContent(json, Encoding.UTF8);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = client.PostAsync(new Uri(url), httpContent).Result;

            if (response.StatusCode == HttpStatusCode.OK) return true;
            var result = response.Content.ReadAsStringAsync().Result;
            InternalLogger.Debug(
                $"Unable to send to Azure Log Analytics || Response: ({response.StatusCode.ToString()}){result}");
        }
        catch (Exception e)
        {
            InternalLogger.Error(e, "Unable to post to Azure Log Analytics: " + e.Message);
        }

        return false;
    }

    private string GetSignature(string method, int contentLength, string contentType, string date, string resource)
    {
        var message = $"{method}\n{contentLength}\n{contentType}\nx-ms-date:{date}\n{resource}";
        return BuildSecret(message, _sharedKey);
    }

    private static string BuildSecret(string message, string secret)
    {
        var encoding = new ASCIIEncoding();
        var keyByte = Convert.FromBase64String(secret);
        var messageBytes = encoding.GetBytes(message);
        using var hmacSha256 = new HMACSHA256(keyByte);
        var hash = hmacSha256.ComputeHash(messageBytes);
        return Convert.ToBase64String(hash);
    }
}
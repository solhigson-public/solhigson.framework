using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Solhigson.Framework.Web.Api
{
    public class ApiRequestDetails
    {
        public ApiRequestDetails(Uri uri, HttpMethod httpMethod, string payload = null)
        {
            Uri = uri;
            TimeOut = 0;
            Format = ApiRequestService.ContentTypeJson;
            HttpMethod = httpMethod;
            Payload = payload;
            ExpectContinue = true;
        }

        public bool ExpectContinue { get; set; }
        public Uri Uri { get; }
        public HttpMethod HttpMethod { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Format { get; set; }
        public int TimeOut { get; set; }
        public string Payload { get; }
        public string ServiceName { get; set; }
        public string ServiceType { get; set; }
        public string ServiceDescription { get; set; }
    }
}
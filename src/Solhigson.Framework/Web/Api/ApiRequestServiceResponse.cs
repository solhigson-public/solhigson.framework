﻿using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace Solhigson.Framework.Web.Api
{
    public class ApiRequestServiceResponse
    {
        public HttpStatusCode HttpStatusCode { get; set; }

        public string HttpStatusDescription { get; set; }

        public string Response { get; set; }

        public string Request { get; set; }

        public bool IsSuccessful => IsSuccessfulStatusCode((int) HttpStatusCode);

        public HttpResponseMessage HttpResponseMessage { get; set; }

        public Dictionary<string, string> RequestHeaders { get; set; }
        public Dictionary<string, string> ResponseHeaders { get; set; }

        public bool IsTimeout => this.HttpStatusCode == HttpStatusCode.GatewayTimeout ||
                                 this.HttpStatusCode == HttpStatusCode.RequestTimeout;

        private static bool IsSuccessfulStatusCode(int statusCode)
        {
            return statusCode >= 200 && statusCode < 300;
        }
        
        
    }

    public class ApiRequestServiceResponse<T> : ApiRequestServiceResponse
    {
        public T Result { get; set; }
    }
}
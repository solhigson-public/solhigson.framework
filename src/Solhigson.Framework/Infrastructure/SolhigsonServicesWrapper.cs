﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Solhigson.Framework.Web.Api;

namespace Solhigson.Framework.Infrastructure
{
    public class SolhigsonServicesWrapper
    {
        public SolhigsonServicesWrapper(SolhigsonConfigurationCache configurationCache,
            IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiRequestService apiRequestService)
        {
            ConfigurationCache = configurationCache;
            HttpContextAccessor = httpContextAccessor;
            Configuration = configuration;
            ApiRequestService = apiRequestService;
        }


        public SolhigsonConfigurationCache ConfigurationCache { get; }
        public IHttpContextAccessor HttpContextAccessor { get; }
        public IConfiguration Configuration { get; set; }
        public ApiRequestService ApiRequestService { get; set; }
    }
}
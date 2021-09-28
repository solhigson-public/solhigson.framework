using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Solhigson.Framework.Web.Api;

namespace Solhigson.Framework.Infrastructure
{
    internal class SolhigsonServicesWrapper
    {
        public SolhigsonServicesWrapper(SolhigsonAppSettings appSettings,
            IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IApiRequestService apiRequestService)
        {
            AppSettings = appSettings;
            HttpContextAccessor = httpContextAccessor;
            Configuration = configuration;
            ApiRequestService = apiRequestService;
        }


        public SolhigsonAppSettings AppSettings { get; }
        public IHttpContextAccessor HttpContextAccessor { get; }
        public IConfiguration Configuration { get; set; }
        public IApiRequestService ApiRequestService { get; set; }
    }
}
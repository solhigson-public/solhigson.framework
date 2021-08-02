using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Solhigson.Framework.Web.Api;

namespace Solhigson.Framework.Infrastructure
{
    public class SolhigsonServicesWrapper
    {
        public SolhigsonServicesWrapper(SolhigsonConfigurationCache configurationCache,
            IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            ConfigurationCache = configurationCache;
            HttpContextAccessor = httpContextAccessor;
            Configuration = configuration;
        }


        public SolhigsonConfigurationCache ConfigurationCache { get; }
        public IHttpContextAccessor HttpContextAccessor { get; }
        public IConfiguration Configuration { get; set; }
    }
}
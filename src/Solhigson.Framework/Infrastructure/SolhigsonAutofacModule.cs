using Autofac;
using Solhigson.Framework.Services;
using Solhigson.Framework.Web.Api;
using Solhigson.Framework.Web.Middleware;

namespace Solhigson.Framework.Infrastructure
{
    public class SolhigsonAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            #region Registed AsSelf(), no interface implementation

            builder.RegisterType<ConfigurationWrapper>().AsSelf().SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

            builder.RegisterType<SolhigsonConfigurationCache>().AsSelf().SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

            builder.RegisterType<SolhigsonServicesWrapper>().AsSelf().SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

            builder.RegisterType<AzureLogAnalyticsService>().AsSelf().SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

            builder.RegisterType<SolhigsonApiTraceMiddleware>().AsSelf().SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            
            builder.RegisterType<SolhigsonExceptionHandlingMiddleware>().AsSelf().SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            
            #endregion

            builder.RegisterType<ApiRequestService>().As<IApiRequestService>().SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);


        }
    }
}
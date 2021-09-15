using Autofac;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Persistence;
using Solhigson.Framework.Persistence.Repositories;
using Solhigson.Framework.Persistence.Repositories.Abstractions;
using Solhigson.Framework.Web.Api;
using Solhigson.Framework.Web.Middleware;

namespace Solhigson.Framework.Infrastructure
{
    public class SolhigsonAutofacModule : Module
    {
        private readonly string _connectionString;

        public SolhigsonAutofacModule(string connectionString)
        {
            _connectionString = connectionString;
        }
        protected override void Load(ContainerBuilder builder)
        {
            #region Registed AsSelf(), no interface implementation

            if (!string.IsNullOrWhiteSpace(_connectionString))
            {
                builder.Register(c =>
                {
                    var opt = new DbContextOptionsBuilder<SolhigsonDbContext>();
                    opt.UseSqlServer(_connectionString);
                    return new SolhigsonDbContext(opt.Options);
                }).AsSelf().InstancePerLifetimeScope();
            }

            builder.RegisterType<ConfigurationWrapper>().AsSelf().SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

            builder.RegisterType<SolhigsonConfigurationCache>().AsSelf().SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

            builder.RegisterType<SolhigsonServicesWrapper>().AsSelf().SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

            builder.RegisterType<ApiTraceMiddleware>().AsSelf().SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            
            builder.RegisterType<ExceptionHandlingMiddleware>().AsSelf().SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            
            #endregion

            builder.RegisterType<ApiRequestService>().As<IApiRequestService>().SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

            builder.RegisterType<RepositoryWrapper>().As<IRepositoryWrapper>().SingleInstance()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
        }
    }
}
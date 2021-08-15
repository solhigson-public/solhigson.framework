using System;
using System.Data.Common;
using Autofac;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Config;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Solhigson.Framework.Logging.Nlog.Renderers;
using Solhigson.Framework.Mocks;
using Solhigson.Framework.Web.Api;
using Xunit.Abstractions;
using Solhigson.Framework.Logging.Nlog.Targets;
using Solhigson.Framework.Logging.Nlog;
using Solhigson.Framework.Extensions;
using [ProjectRootNamespace].Web;


namespace [ProjectRootNamespace].Tests
{
    //[GeneratedFileComment]
    public partial class TestBase
    {
        private readonly DbConnection _connection;
        public TestBase(ITestOutputHelper testOutputHelper)
        {
            _connection = new SqliteConnection("Filename=:memory:");

            _connection.Open();

            testOutputHelper.ConfigureNLogConsoleOutputTarget();
            var builder = new ContainerBuilder();

            Startup.RegisterDependencies(builder);

            //return builder.Build();
            /*
             * Override certain dependencies for mocking
             */
            
            //IConfiguration
            builder.RegisterInstance(new ConfigurationBuilder().Build()).As<IConfiguration>();
            
            //IHttpContextAccessor
            var httpContext = Substitute.For<IHttpContextAccessor>();
            httpContext.HttpContext.ReturnsNull();
            builder.RegisterInstance(httpContext);
            
            //solhigson framework IApiRequestService
            builder.RegisterInstance(Substitute.For<IApiRequestService>());
            
            //Hangfire IBackgroundJobClient
            builder.RegisterType<MockHangfireBackgroundJobClient>().As<IBackgroundJobClient>()
                .SingleInstance();

            //AppDbContext to use EfCore in memory database
            builder.Register(c =>
            {
                var opt = new DbContextOptionsBuilder<[DbContextNamespace].[DbContextName]>();
                opt.UseSqlite(_connection);
                return new [DbContextNamespace].[DbContextName](opt.Options);
            }).AsSelf().InstancePerLifetimeScope();
            
            //Any other custom dependency overrides
            LoadCustomDependencyOverrides(builder);

            var autofacContainer = builder.Build();
            
            ServicesWrapper = autofacContainer.Resolve<[DtoProjectNamespace].[ServicesFolder].ServicesWrapper>();
            
            //For Arrange and Assert only
            RepositoryWrapper = autofacContainer.Resolve<[PersistenceProjectRootNamespace].[RepositoriesFolder].[AbstractionsFolder].IRepositoryWrapper>();
            
            //Ensure sqlite db refreshed
            autofacContainer.Resolve<[DbContextNamespace].[DbContextName]>().Database.EnsureDeleted();
            autofacContainer.Resolve<[DbContextNamespace].[DbContextName]>().Database.EnsureCreated();
        }
        
        public void Dispose() => _connection.Dispose();

        
        private partial void LoadCustomDependencyOverrides(ContainerBuilder builder);

        public [DtoProjectNamespace].[ServicesFolder].ServicesWrapper ServicesWrapper { get; }
        public [PersistenceProjectRootNamespace].[RepositoriesFolder].[AbstractionsFolder].IRepositoryWrapper RepositoryWrapper { get; }

    }
}
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
using Solhigson.Framework.Persistence;
using [ProjectRootNamespace].Web;


namespace [ProjectRootNamespace].Tests
{
    [GeneratedFileComment]
    public partial class TestBase
    {
        private readonly DbConnection _connection;
        public TestBase(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.ConfigureNLogConsoleOutputTarget();
            var builder = new ContainerBuilder();

            Startup.RegisterDependencies(builder);

            /*
             * Override certain dependencies for mocking
             */
            //IConfiguration
            builder.RegisterInstance(new ConfigurationBuilder().Build()).As<IConfiguration>().SingleInstance();
            
            //IHttpContextAccessor
            var httpContext = Substitute.For<IHttpContextAccessor>();
            httpContext.HttpContext.ReturnsNull();
            builder.RegisterInstance(httpContext).SingleInstance();
            
            //Solhigson.Framework IApiRequestService
            builder.RegisterInstance(Substitute.For<IApiRequestService>()).SingleInstance();
            
            //Hangfire IBackgroundJobClient
            builder.RegisterType<MockHangfireBackgroundJobClient>().As<IBackgroundJobClient>()
                .SingleInstance();

            //[DbContextName] to use EfCore in sqlite in memory database
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();
            builder.Register(c =>
            {
                var opt = new DbContextOptionsBuilder<[DbContextNamespace].[DbContextName]>();
                opt.UseSqlite(_connection);
                return new [DbContextNamespace].[DbContextName](opt.Options);
            }).AsSelf().InstancePerLifetimeScope();
            
            //Any other custom dependency overrides - implementation in BaseTest.cs
            LoadCustomDependencyOverrides(builder);

            var autofacContainer = builder.Build();
            
            ServicesWrapper = autofacContainer.Resolve<[DtoProjectNamespace].[ServicesFolder].ServicesWrapper>();
            
            // *** For Arrange and Assert only!!! ***
            RepositoryWrapper = autofacContainer.Resolve<[PersistenceProjectRootNamespace].[RepositoriesFolder].[AbstractionsFolder].IRepositoryWrapper>();
            
            //Ensure sqlite db is refreshed
            var dbContext = autofacContainer.Resolve<[DbContextNamespace].[DbContextName]>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }
        
        //Use method implemention in BaseTest.cs to include other DI registrations
        private partial void LoadCustomDependencyOverrides(ContainerBuilder builder);

        public [DtoProjectNamespace].[ServicesFolder].ServicesWrapper ServicesWrapper { get; }
    
        // *** For Arrange and Assert only!!! ***
        public [PersistenceProjectRootNamespace].[RepositoriesFolder].[AbstractionsFolder].IRepositoryWrapper RepositoryWrapper { get; }

        public void Dispose() => _connection.Dispose();

    }
}
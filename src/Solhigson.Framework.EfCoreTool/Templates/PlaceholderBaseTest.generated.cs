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
using Microsoft.Extensions.DependencyInjection;
using Autofac.Extensions.DependencyInjection;

namespace [ProjectRootNamespace].Tests
{
    [GeneratedFileComment]
    public partial class TestBase
    {
        protected IServiceProvider ServiceProvider { get; private set; }
        private readonly DbConnection _connection;
        public TestBase(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.ConfigureNLogConsoleOutputTarget();
            var builder = new ContainerBuilder();

            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();
            RegisterStartUpDependencies(builder, services, configuration);

            /*
             * Override certain dependencies for mocking
             */
            //IConfiguration
            builder.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();
            
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
            
            builder.Register(c =>
            {
                var opt = new DbContextOptionsBuilder<SolhigsonDbContext>();
                opt.UseSqlite(_connection);
                return new SolhigsonDbContext(opt.Options);
            }).AsSelf().InstancePerLifetimeScope();

            
            //Any other custom dependency overrides - implementation in BaseTest.cs
            LoadCustomDependencyOverrides(builder, services, configuration);

            builder.Populate(services);
            ServiceProvider = new AutofacServiceProvider(builder.Build());
            
            ServicesWrapper = ServiceProvider.GetRequiredService<[DtoProjectNamespace].[ServicesFolder].ServicesWrapper>();
            
            // *** For Arrange and Assert only!!! ***
            RepositoryWrapper = ServiceProvider.GetRequiredService<[PersistenceProjectRootNamespace].[RepositoriesFolder].[AbstractionsFolder].IRepositoryWrapper>();
            
            //Ensure sqlite db is refreshed
            var dbContext = ServiceProvider.GetRequiredService<[DbContextNamespace].[DbContextName]>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var solhigsonDbContext = ServiceProvider.GetRequiredService<SolhigsonDbContext>();
            solhigsonDbContext.Database.ExecuteSqlRaw(solhigsonDbContext.Database.GenerateCreateScript());

            InitializeData();
        }
        
        //Use method implemention in BaseTest.cs to include other DI registrations
        protected virtual void LoadCustomDependencyOverrides(ContainerBuilder builder, IServiceCollection services,
            IConfiguration configuration)
        {
                
        }

        protected virtual void RegisterStartUpDependencies(ContainerBuilder builder, IServiceCollection services,
            IConfiguration configuration)
        {
                
        }

        protected virtual void InitializeData()
        {
                
        }


        public [DtoProjectNamespace].[ServicesFolder].ServicesWrapper ServicesWrapper { get; }
    
        // *** For Arrange and Assert only!!! ***
        public [PersistenceProjectRootNamespace].[RepositoriesFolder].[AbstractionsFolder].IRepositoryWrapper RepositoryWrapper { get; }

        public void Dispose() => _connection.Dispose();

    }
}
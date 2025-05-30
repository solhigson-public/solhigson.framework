﻿using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Solhigson.Framework.Persistence;
using Solhigson.Framework.Persistence.Repositories;
using Solhigson.Framework.Persistence.Repositories.Abstractions;
using Solhigson.Framework.Services;
using Solhigson.Framework.Services.Abstractions;
using Solhigson.Framework.Web.Api;
using Solhigson.Framework.Web.Middleware;

namespace Solhigson.Framework.Infrastructure;

public class SolhigsonAutofacModule : Module
{
    private readonly string? _connectionString;
    private readonly IConfiguration _configuration;

    public SolhigsonAutofacModule(IConfiguration configuration, string? connectionString)
    {
        _connectionString = connectionString;
        _configuration = configuration;
    }

    public static void LoadDbSupport(ContainerBuilder builder, IConfiguration configuration,
        DbContextOptionsBuilder<SolhigsonDbContext>? optionsBuilder = null)
    {
        builder.RegisterType<RepositoryWrapper>().As<IRepositoryWrapper>().InstancePerLifetimeScope()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

        builder.RegisterType<SolhigsonConfigurationService>().AsSelf().InstancePerLifetimeScope()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

        builder.Register(c => new ConfigurationWrapper(configuration, optionsBuilder))
            .AsSelf().InstancePerLifetimeScope();

        builder.RegisterType<CurrentLogScopedPropertiesAccessor>().AsSelf().InstancePerLifetimeScope();
    }
    
    protected override void Load(ContainerBuilder builder)
    {
        #region Registed AsSelf(), no interface implementation

        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            var opt = new DbContextOptionsBuilder<SolhigsonDbContext>();
            opt.UseSqlServer(_connectionString);
            builder.Register(c => new SolhigsonDbContext(opt.Options)).AsSelf().InstancePerLifetimeScope();
                
            LoadDbSupport(builder, _configuration, opt);
        }
        else
        {
            builder.Register(c => new ConfigurationWrapper(_configuration, null))
                .AsSelf().InstancePerLifetimeScope();
        }
        /*
        /*
        builder.Register(c => new ConfigurationWrapper(_configuration, _connectionString))
            .AsSelf().InstancePerLifetimeScope();
            #1#

        builder.RegisterType<ConfigurationWrapper>().AsSelf().InstancePerLifetimeScope()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            */

        builder.RegisterType<ApiTraceMiddleware>().AsSelf().SingleInstance()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            
        builder.RegisterType<ExceptionHandlingMiddleware>().AsSelf().InstancePerLifetimeScope()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            
        #endregion

        builder.RegisterType<ApiRequestService>().As<IApiRequestService>().InstancePerLifetimeScope()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

        builder.RegisterType<NotificationService>().As<INotificationService>().InstancePerLifetimeScope()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

    }
}
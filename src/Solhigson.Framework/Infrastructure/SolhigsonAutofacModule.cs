using System;
using System.Linq;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Solhigson.Framework.AuditCapture;
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
    private readonly Action<DbContextOptionsBuilder<SolhigsonDbContext>>? _configureDbContext;

    public SolhigsonAutofacModule(IConfiguration configuration, string? connectionString)
    {
        _connectionString = connectionString;
        _configuration = configuration;
    }

    public SolhigsonAutofacModule(IConfiguration configuration,
        Action<DbContextOptionsBuilder<SolhigsonDbContext>> configureDbContext)
    {
        _configuration = configuration;
        _configureDbContext = configureDbContext;
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

        if (_configureDbContext is not null)
        {
            var opt = new DbContextOptionsBuilder<SolhigsonDbContext>();
            _configureDbContext(opt);
            if (!opt.Options.Extensions.OfType<RelationalOptionsExtension>().Any())
            {
                throw new InvalidOperationException(
                    "RegisterSolhigsonDependenciesWithProvider: the configureDbContext delegate did not configure a database provider. " +
                    "Call opt.UseNpgsql(...) or opt.UseSqlServer(...) inside the delegate.");
            }
            builder.Register(c => new SolhigsonDbContext(opt.Options)).AsSelf().InstancePerLifetimeScope();
            LoadDbSupport(builder, _configuration, opt);
        }
        else if (!string.IsNullOrWhiteSpace(_connectionString))
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

        builder.RegisterInstance(new ApiConfiguration()).AsSelf().SingleInstance()
            .PreserveExistingDefaults();

        builder.RegisterType<ApiRequestService>().As<IApiRequestService>().InstancePerLifetimeScope()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

        builder.RegisterType<NotificationService>().As<INotificationService>().InstancePerLifetimeScope()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

        // Audit-trail seams (F1). Both use PreserveExistingDefaults() so a consumer's own
        // registration (a web/Hangfire actor provider, a pre-configured capture registry) wins.
        builder.RegisterType<UnattributedAuditActorProvider>().As<IAuditActorProvider>().SingleInstance()
            .PreserveExistingDefaults();

        builder.RegisterType<AuditCaptureRegistry>().AsSelf().SingleInstance()
            .PreserveExistingDefaults();

        // Audit-capture interceptors (F2). Registered for AVAILABILITY only — the consumer wires them
        // onto its own AppDbContext via AddInterceptors (E2). They are NOT auto-wired onto
        // SolhigsonDbContext, whose fixed model can never contain AuditTrail (the capture interceptor's
        // FindEntityType gate would no-op there anyway). SingleInstance because a pooled DbContext
        // captures the interceptor across scopes; both interceptors hold only singleton-safe, read-only
        // seams (no per-save instance state). The masking overlay is a consumer-overridable singleton.
        builder.RegisterType<AuditCaptureOptions>().AsSelf().SingleInstance()
            .PreserveExistingDefaults();

        builder.RegisterType<AuditCaptureSaveChangesInterceptor>().AsSelf().SingleInstance();

        builder.RegisterType<AuditTrailAppendOnlyInterceptor>().AsSelf().SingleInstance();

        // Explicit audit logging (F3). Open generic so the consumer binds its OWN DbContext
        // (IAuditTrailService<AppDbContext>) — the same availability-only posture as the F2
        // interceptors above: SolhigsonDbContext never maps AuditTrail, so a framework-context
        // binding no-ops (with a warning) at the service's FindEntityType gate.
        // InstancePerLifetimeScope because the service holds the resolved (scoped) TContext.
        // Consumer-override symmetry with the F1/F2 PreserveExistingDefaults() block is carried by
        // Autofac's open-generic semantics instead (the API has no PreserveExistingDefaults for
        // DynamicRegistrationStyle): an explicitly registered closed IAuditTrailService<TContext>
        // always beats this open generic, and a consumer's own open-generic registration made after
        // RegisterModule becomes the default.
        builder.RegisterGeneric(typeof(AuditTrailService<>)).As(typeof(IAuditTrailService<>))
            .InstancePerLifetimeScope();
    }
}
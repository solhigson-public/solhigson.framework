using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets.Wrappers;
using Polly;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Caching;
using Solhigson.Framework.Dto;
using Solhigson.Framework.Identity;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Infrastructure.Dependency;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Logging.Nlog;
using Solhigson.Framework.Logging.Nlog.Dto;
using Solhigson.Framework.Logging.Nlog.Renderers;
using Solhigson.Framework.Logging.Nlog.Targets;
using Solhigson.Framework.Notification;
using Solhigson.Framework.Services;
using Solhigson.Framework.Utilities;
using Solhigson.Framework.Utilities.Extensions;
using Solhigson.Framework.Utilities.Linq;
using Solhigson.Framework.Utilities.Security;
using Solhigson.Framework.Web;
using Solhigson.Framework.Web.Api;
using Solhigson.Framework.Web.Middleware;
using Xunit.Abstractions;
using LogLevel = NLog.LogLevel;
using LogManager = Solhigson.Framework.Logging.LogManager;

namespace Solhigson.Framework.Extensions;

public static class Extensions
{
    private static readonly LogWrapper Logger = LogManager.GetLogger(typeof(Extensions).FullName);
        
    #region Api Extensions

    public static Dictionary<string, string>? AddAuthorizationHeader(this Dictionary<string, string>? headers,
        string type, string value)
    {
        headers?.Add("Authorization", $"{type} {value}");
        return headers;
    }

    #endregion

    #region Application Startup

      
    public static ContainerBuilder RegisterSolhigsonDependencies(this ContainerBuilder builder, IConfiguration configuration, string? connectionString = null)
    {
        builder.RegisterModule(new SolhigsonAutofacModule(configuration, connectionString));
        return builder;
    }
        
    /// <summary>
    /// Registers types in specified assembly that implements <see cref="IDependencyInject"/> using
    /// <see cref="DependencyInjectAttribute"/> attributes to detemine scope (optional - defaults to scoped or LifeTimescope)
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="assembly"></param>
    /// <returns></returns>
    public static ContainerBuilder RegisterIndicatedDependencies(this ContainerBuilder builder, Assembly? assembly)
    {
        if (assembly is null)
        {
            return builder;
        }
        var coreServicesAssemblyTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false });
        var injectType = typeof(IDependencyInject);
        foreach (var type in coreServicesAssemblyTypes)
        {
            if (injectType.IsAssignableFrom(type))
            {
                var regBuilder = builder.RegisterType(type).AsSelf().PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
                var attr = type.GetCustomAttribute<DependencyInjectAttribute>();
                var dType = DependencyType.Scoped.ToString();
                if (attr is not null)
                {
                    dType = attr.DependencyType.ToString();
                }
                Logger.LogDebug($"Registering Dependency: {type.FullName} as {dType}");
                switch (attr?.DependencyType)
                {
                    case DependencyType.Singleton:
                        regBuilder.SingleInstance();
                        break;
                    case DependencyType.NewInstance:
                        regBuilder.InstancePerDependency();
                        break;
                    default:
                        regBuilder.InstancePerLifetimeScope();
                        break;
                }
            }
        }
        return builder;
    }
        
    // public static IApplicationBuilder UseSolhigsonCacheManager(this IApplicationBuilder app, string connectionString, int cacheDependencyChangeTrackerTimerIntervalMilliseconds = 5000,
    //     int cacheExpirationPeriodMinutes = 1440, Assembly? databaseModelsAssembly = null, bool continueOnError = true)
    // {
    //     CacheManager.Initialize(connectionString, cacheDependencyChangeTrackerTimerIntervalMilliseconds, cacheExpirationPeriodMinutes, databaseModelsAssembly,
    //         continueOnError);
    //     return app;
    // }

    public static IApplicationBuilder ConfigureSolhigsonNLogDefaults(this IApplicationBuilder app,
        DefaultNLogParameters? defaultNLogParameters = null)
    {
        defaultNLogParameters ??= new DefaultNLogParameters();
        defaultNLogParameters.ApiTraceConfiguration ??= configuration =>
        {
            configuration.LogOutBoundApiRequests = configuration.LogInBoundApiRequests = defaultNLogParameters.LogApiTrace;
        };
        
        var apiConfiguration = new ApiConfiguration();
        defaultNLogParameters.ApiTraceConfiguration.Invoke(apiConfiguration);
        
        if (apiConfiguration.LogInBoundApiRequests)
        {
            app.UseMiddleware<ApiTraceMiddleware>();
        }

        app.ApplicationServices.GetService<IApiRequestService>()?.UseConfiguration(defaultNLogParameters.ApiTraceConfiguration);

        ServiceProviderWrapper.ServiceProvider = app.ApplicationServices;
        return app;
    }

    private static object CreateInstance(Type type, string protectedFields)
    {
        if (type == typeof(CustomDataRenderer) || type.Name == nameof(CustomDataRenderer))
        {
            return new CustomDataRenderer(protectedFields);
        }
        if (type == typeof(CustomDataRenderer2) || type.Name == nameof(CustomDataRenderer2))
        {
            return new CustomDataRenderer2(protectedFields);
        }

        return Activator.CreateInstance(type);
    }

    public static IApplicationBuilder UseSolhigsonNLogDefaultFileTarget(this IApplicationBuilder app,
        DefaultNLogParameters defaultNLogParameters = null)
    {
        app.ConfigureSolhigsonNLogDefaults(defaultNLogParameters);
        defaultNLogParameters ??= new DefaultNLogParameters();
            
        var config = new LoggingConfiguration();
        config.AddRule(LogLevel.Info, LogLevel.Error, NLogDefaults.GetDefaultFileTarget(defaultNLogParameters.EncodeChildJsonContent));
        NLog.LogManager.Configuration = config;
        LogManager.SetLogLevel(defaultNLogParameters.LogLevel);
        return app;
    }
        
    public static IApplicationBuilder UseSolhigsonNLogCustomTarget(this IApplicationBuilder app,
        [NotNull] CustomNLogTargetParameters customNLogTargetParameters)
    {
        if (customNLogTargetParameters == null)
        {
            app.UseSolhigsonNLogDefaultFileTarget();
            InternalLogger.Error(
                "Unable to initalize Custom NLog Target because one or more the the required parameters are missing: " +
                "[WorkspaceId, Sharedkey or LogName].");
            return app;
        }

        var config = new LoggingConfiguration();
        var fallbackGroupTarget = new FallbackGroupTarget
        {
            Name = "FallBackTargets",
        };

        fallbackGroupTarget.Targets.Add(customNLogTargetParameters.Target);
        fallbackGroupTarget.Targets.Add(NLogDefaults.GetDefaultFileTarget(customNLogTargetParameters.EncodeChildJsonContent, true));
        config.AddRule(LogLevel.Info, LogLevel.Error, fallbackGroupTarget);

        NLog.LogManager.Configuration = config;
        LogManager.SetLogLevel(customNLogTargetParameters.LogLevel);

        return app;
    }

        
    public static void ConfigureNLogConsoleOutputTarget(this ITestOutputHelper outputHelper)
    {
        ConfigurationItemFactory.Default.RegisterItemsFromAssembly(typeof(CustomDataRenderer).Assembly);
        ConfigurationItemFactory.Default.CreateInstance = type => CreateInstance(type, null);

        var config = new LoggingConfiguration();
        var testOutputHelperTarget = new XUnitTestOutputHelperTarget(outputHelper)
        {
            Name = "TestsOutput",
            Layout = NLogDefaults.TestsLayout
        };
        config.AddRule(LogLevel.Info, LogLevel.Error, testOutputHelperTarget);
        NLog.LogManager.Configuration = config;
    }

    public static IApplicationBuilder UseSolhigsonNLogAzureLogAnalyticsTarget(this IApplicationBuilder app,
        DefaultNLogAzureLogAnalyticsParameters parameters = null)
    {
        if (string.IsNullOrWhiteSpace(parameters?.AzureAnalyticsWorkspaceId)
            || string.IsNullOrWhiteSpace(parameters?.AzureAnalyticsSharedSecret)
            || string.IsNullOrWhiteSpace(parameters?.AzureAnalyticsLogName))
        {
            app.UseSolhigsonNLogDefaultFileTarget();
            InternalLogger.Error(
                "Unable to initalize NLog Azure Analytics Target because one or more the the required parameters are missing: " +
                "[WorkspaceId, Sharedkey or LogName].");
            return app;
        }
        app.ConfigureSolhigsonNLogDefaults(parameters);

        var customTarget = new AzureLogAnalyticsTarget(parameters.AzureAnalyticsWorkspaceId, 
            parameters.AzureAnalyticsSharedSecret, parameters.AzureAnalyticsLogName,
            app.ApplicationServices.GetRequiredService<IHttpClientFactory>())
        {
            Name = "custom document",
            Layout = NLogDefaults.GetDefaultJsonLayout(),
        };
        
        var customTargetParameters = new CustomNLogTargetParameters(customTarget);
        parameters.Adapt(customTargetParameters);
        app.UseSolhigsonNLogCustomTarget(customTargetParameters);
        return app;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="smtpConfiguration"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="Exception"></exception>
    public static IServiceCollection AddSolhigsonSmtpMailProvider(this IServiceCollection services)
    {
        services.AddSingleton<IMailProvider, SolhigsonSmtpMailProvider>();
        return services;
    }

    public static IServiceCollection AddSolhigsonDefaultHttpClient(this IServiceCollection services)
    {
        IAsyncPolicy<HttpResponseMessage> ConfigurePolicy(PolicyBuilder<HttpResponseMessage> builder) =>
            builder.WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)
                }, 
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    LogManager.GetLogger("HttpPollyService")
                        .LogWarning($"Delaying for {timespan.TotalMilliseconds}ms, then making retry {retryAttempt}.");
                });

        services.AddHttpClient(AzureLogAnalyticsService.AzureLogAnalyticsNamedHttpClient)
            .AddTransientHttpErrorPolicy(ConfigurePolicy);

        return services;
    }

    private static IServiceCollection AddSolhigsonIdentityManager<TUser, TRole, TRoleGroup, TKey, TContext>(this IServiceCollection services,
        Action<IdentityOptions>? setupAction = null) 
        where TUser : SolhigsonUser<TKey, TRole>
        where TContext : SolhigsonIdentityDbContext<TUser, TRole, TKey>
        where TRole : SolhigsonAspNetRole<TKey>, new()
        where TRoleGroup : SolhigsonRoleGroup, new()
        where TKey : IEquatable<TKey>
    {
        services.AddIdentity<TUser, TRole>(setupAction).AddEntityFrameworkStores<TContext>()
            .AddDefaultTokenProviders();
        //services.TryAddScoped<SolhigsonIdentityManager<TUser, TRoleGroup, TRole, TContext, TKey>>();
        services.TryAddScoped<RoleGroupManager<TRoleGroup, TRole, TUser, TContext, TKey>>();
        services.TryAddScoped<PermissionManager<TUser, TRole, TContext, TKey>>();
        services.TryAddScoped<IPermissionMiddleware, PermissionsMiddleware<TUser, TRole, TKey, TContext>>();
        return services;
    }
        
    public static IServiceCollection AddSolhigsonIdentityManager<TUser, TContext>(this IServiceCollection services,
        Action<IdentityOptions> setupAction = null) 
        where TUser : SolhigsonUser<string, SolhigsonAspNetRole>
        where TContext : SolhigsonIdentityDbContext<TUser, SolhigsonAspNetRole, string>
    {
        services.AddSolhigsonIdentityManager<TUser, SolhigsonAspNetRole, SolhigsonRoleGroup, string, TContext>(setupAction);
        services.TryAddScoped<SolhigsonIdentityManager<TUser, TContext>>();
        return services;
    }
        
    public static IServiceCollection AddSolhigsonIdentityManager<TUser, TKey, TContext>(this IServiceCollection services,
        Action<IdentityOptions> setupAction = null) 
        where TUser : SolhigsonUser<TKey>
        where TContext : SolhigsonIdentityDbContext<TUser, SolhigsonAspNetRole<TKey>, TKey>
        where TKey : IEquatable<TKey>
    {
        services.AddSolhigsonIdentityManager<TUser, SolhigsonAspNetRole<TKey>, SolhigsonRoleGroup, TKey, TContext>(setupAction);
        services.TryAddScoped<SolhigsonIdentityManager<TUser, TKey, TContext>>();
        return services;
    }
        
    public static IApplicationBuilder UseSolhigsonPermissionMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<IPermissionMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseSolhigsonSmtpProvider(this IApplicationBuilder app,
        Action<SmtpConfiguration> configuration)
    {
        if (configuration is null)
        {
            return app;
        }
        if (app.ApplicationServices.GetRequiredService<IMailProvider>() is not SolhigsonSmtpMailProvider provider)
        {
            throw new Exception(
                $"{nameof(SolhigsonSmtpMailProvider)} service has not been registered, kindly include " +
                $"services.AddSolhigsonSmtpMailProvider() under ConfigureServices in Startup.");
        }
        provider.UseConfiguration(configuration);

        return app;
    }




    #endregion

    #region Identity & Jwt
    public static string GetClaimValue(this IIdentity identity, string claimType)
    {
        var claimIdentity = (ClaimsIdentity) identity;
        return claimIdentity?.Claims?.FirstOrDefault(c => c.Type == claimType)?.Value;
    }
        
    public static string GetEmailClaim(this IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor?.HttpContext?.User?.Identity?.GetClaimValue(ClaimTypes.Email);
    }
        
    public static (string Token, double ExpireTimestamp) GenerateJwtToken(this IEnumerable<Claim> claims, string key, double expirationMinutes,
        string algorithm = SecurityAlgorithms.HmacSha512)
    {
        return CryptoHelper.GenerateJwtToken(claims, key, expirationMinutes, algorithm);
    }
        
    public static string Email(this ClaimsPrincipal principal)
    {
        return principal?.Identity.GetClaimValue(ClaimTypes.Email);
    }

    public static string Id(this ClaimsPrincipal principal)
    {
        return principal?.Identity.GetClaimValue(ClaimTypes.NameIdentifier);
    }

    public static string Role(this ClaimsPrincipal principal)
    {
        return principal?.Identity.GetClaimValue(ClaimTypes.Role);
    }




    public static ClaimsPrincipal? GetPrincipal(string jwtTokenString, string secret,
        TokenValidationParameters? validationParameters,
        string? issuer, string? audience = null)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = (JwtSecurityToken)tokenHandler.ReadToken(jwtTokenString);
            if (jwtToken == null)
                return null;
            var key = Encoding.UTF8.GetBytes(secret);
            validationParameters ??= new TokenValidationParameters
            {
                ValidIssuer = issuer,
                ValidAudience = audience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };
            return tokenHandler.ValidateToken(jwtTokenString,
                validationParameters, out _);
        }
        catch(Exception e)
        {
            Logger.LogError(e);
        }

        return null;
    }

    #endregion


    #region Misc

    public static string CallerIp(this HttpContext httpContext)
    {
        return HelperFunctions.GetCallerIp(httpContext);
    }

    #endregion
        
        
        
    #region DateTime
        
    public static string ToClientTime(this DateTime dt, string format = null)
    {
        return dt.AddMinutes(LocaleUtil.GetTimeZoneOffset()).ToString(format);
    }
        
    public static string ToClientTime(this DateTime? dt, string format = null)
    {
        return dt.HasValue ? dt.Value.ToClientTime(format) : "-";
    }
        
    public static DateTime ToClientDateTime(this DateTime dt, int offSet)
    {
        return dt.AddMinutes(offSet);
    }

    public static DateTime ToClientDateTime(this DateTime? dt, int offSet)
    {
        return dt?.ToClientDateTime(offSet) ?? DateTime.UtcNow.ToClientDateTime(offSet);
    }

    public static DateTime ToClientDateTime(this DateTime dt)
    {
        return dt.AddMinutes(LocaleUtil.GetTimeZoneOffset());
    }
        
    public static DateTime ToClientDateTime(this DateTime? dt)
    {
        return dt?.ToClientDateTime() ?? DateTime.UtcNow.ToClientDateTime();
    }

        
    #endregion

    #region Mvc

    internal static bool ViewExists(this Controller controller, string name)
    {
        var services = controller.HttpContext.RequestServices;
        var viewEngine = services.GetService<ICompositeViewEngine>();
        if (viewEngine is null)
        {
            return false;
        }
        var result = viewEngine.GetView(null, name, true);
        if (!result.Success)
            result = viewEngine.FindView(controller.ControllerContext, name, true);
        return result.Success;
    }
        
    public static void SetDisplayMessage(this SolhigsonMvcControllerBase controller, string message, PageMessageType messageType,
        bool closeOnClick = true,
        bool clearBeforeAdd = false, bool encodeHtml = true)
    {
        SetDisplayMessage(controller.TempData, message, messageType, closeOnClick, clearBeforeAdd, encodeHtml);
    }

    public static List<PageMessage> GetDisplayMessages(this ITempDataDictionary tempData)
    {
        var serializedMessages = tempData[PageMessage.MessageKey];

        return serializedMessages == null
            ? new List<PageMessage>()
            : ((string)serializedMessages).DeserializeFromJson<List<PageMessage>>();
    }

    private static void SetDisplayMessages(this ITempDataDictionary tempData, List<PageMessage> messages)
    {
        tempData[PageMessage.MessageKey] = messages.SerializeToJson();
    }

    public static void SetDisplayMessage(this ITempDataDictionary tempData, string message,
        PageMessageType messageType,
        bool closeOnClick = true,
        bool clearBeforeAdd = false, bool encodeHtml = true)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }
            
        var messages = tempData.GetDisplayMessages();

        if (clearBeforeAdd)
        {
            messages.Clear();
        }

        if (messages.All(t => string.Compare(t.Message, message, StringComparison.OrdinalIgnoreCase) != 0))
        {
            messages.Add(new PageMessage
            {
                Message = message,
                Type = messageType,
                CloseOnClick = closeOnClick,
                EncodeHtml = encodeHtml,
            });
        }

        tempData.SetDisplayMessages(messages);
    }

    public static string GetHeaderValue(this ControllerBase controller, string header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }
        string val = null;
        if (controller.Request.Headers.TryGetValue(header, out var headerValue))
        {
            val = headerValue.FirstOrDefault();
        }
        return val;
    }


    #endregion
        
    public static bool IsAsyncMethod(this MethodInfo method)
    {
        return method?.GetCustomAttribute<AsyncStateMachineAttribute>() != null;
    }
        
    public static IActionResult HttpOk(this ResponseInfo response)
    {
        return new OkObjectResult(response);
    }
        
    public static IActionResult HttpOk<T>(this ResponseInfo<T> response)
    {
        if (!response.IsSuccessful && response.Data is null)
        {
            //for unit test simplification
            return response.InfoResult.HttpOk();
        }
        return new OkObjectResult(response);
    }

    public static bool IsApiController(this HttpContext context)
    {
        return context.GetEndpoint()?.Metadata
            .GetMetadata<ControllerActionDescriptor>()?.IsApiController() == true;
    }
    
    public static bool IsApiController(this ControllerActionDescriptor type)
    {
        return type?.ControllerTypeInfo?.GetCustomAttribute<ApiControllerAttribute>() != null;
    }


    /*
    public static bool IsPermissionAllowed(this SolhigsonMvcControllerBase controller, string permission)
    {
        return controller.SolhigsonServicesWrapper.PermissionService
            .VerifyPermission(permission, controller.User).IsSuccessful;
    }

    public static bool IsPermissionAllowed(this SolhigsonApiControllerBase controller, string permission)
    {
        return controller.SolhigsonServicesWrapper.PermissionService
            .VerifyPermission(permission, controller.User).IsSuccessful;
    }
    */
        
    private static SolhigsonMvcControllerBase GetController(this IRazorPage view)
    {
        if (view.ViewContext.ActionDescriptor is not ControllerActionDescriptor cont)
        {
            return null;
        }

        if (view.ViewContext.HttpContext.RequestServices.GetService(cont.ControllerTypeInfo) is SolhigsonMvcControllerBase
            baseController)
        {
            return baseController;
        }

        return null;
    }

    public static bool IsValidDate(this DateTime dateTime)
    {
        return dateTime != DateTime.MinValue && dateTime != DateTime.MaxValue;
    }
        
    
    public static string Truncate(this string text, int length)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }
        var maxLength = text.Length > length ? length : text.Length;
        if (text.Length > maxLength)
        {
            text = text[..maxLength] + "...";
        }

        return text;

    }
    
    public static bool HasData<T>(this IEnumerable<T>? data)
    {
        return data is not null && data.Any();
    }



}
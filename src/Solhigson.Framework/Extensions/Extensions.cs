using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Authorization;
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
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using Polly;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Caching;
using Solhigson.Framework.Identity;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Logging.Nlog;
using Solhigson.Framework.Logging.Nlog.Dto;
using Solhigson.Framework.Logging.Nlog.Renderers;
using Solhigson.Framework.Logging.Nlog.Targets;
using Solhigson.Framework.Notification;
using Solhigson.Framework.Services;
using Solhigson.Framework.Utilities;
using Solhigson.Framework.Utilities.Linq;
using Solhigson.Framework.Utilities.Security;
using Solhigson.Framework.Web;
using Solhigson.Framework.Web.Api;
using Solhigson.Framework.Web.Middleware;
using Xunit.Abstractions;
using LogLevel = NLog.LogLevel;

namespace Solhigson.Framework.Extensions
{
    public static class Extensions
    {
        private static readonly LogWrapper Logger = LogManager.GetCurrentClassLogger();
        
        #region Api Extensions

        public static Dictionary<string, string> AddAuthorizationHeader(this Dictionary<string, string> headers,
            string type, string value)
        {
            headers?.Add("Authorization", $"{type} {value}");
            return headers;
        }

        #endregion

        #region Application Startup

      
        public static ContainerBuilder RegisterSolhigsonDependencies(this ContainerBuilder builder, IConfiguration configuration)
        {
            builder.RegisterModule(new SolhigsonAutofacModule(configuration, configuration.GetConnectionString("DbConnection")));
            return builder;
        }
        
        public static IApplicationBuilder UseSolhigsonCacheManager(this IApplicationBuilder app, string connectionString, int cacheDependencyChangeTrackerTimerIntervalMilliseconds = 5000,
            int cacheExpirationPeriodMinutes = 1440, Assembly databaseModelsAssembly = null)
        {
            CacheManager.Initialize(connectionString, cacheDependencyChangeTrackerTimerIntervalMilliseconds, cacheExpirationPeriodMinutes, databaseModelsAssembly);
            return app;
        }

        public static IApplicationBuilder ConfigureSolhigsonNLogDefaults(this IApplicationBuilder app,
            DefaultNLogParameters defaultNLogParameters = null)
        {
            defaultNLogParameters ??= new DefaultNLogParameters();
            if (defaultNLogParameters.LogApiTrace)
            {
                app.UseMiddleware<ApiTraceMiddleware>();
            }
            ConfigurationItemFactory.Default.CreateInstance = type =>
                type == typeof(CustomDataRenderer) || type.Name == nameof(CustomDataRenderer)
                    ? new CustomDataRenderer(defaultNLogParameters.ProtectedFields)
                    : Activator.CreateInstance(type);
            
            Constants.HttpContextAccessor = app.ApplicationServices.GetService<IHttpContextAccessor>();
            return app;
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
            ConfigurationItemFactory.Default.CreateInstance = type =>
                type == typeof(CustomDataRenderer)
                    ? new CustomDataRenderer(null)
                    : Activator.CreateInstance(type);
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
            DefaultNLogAzureLogAnalyticsParameters defaultNLogAzureLogAnalyticsParameters = null)
        {
            if (string.IsNullOrWhiteSpace(defaultNLogAzureLogAnalyticsParameters?.AzureAnalyticsWorkspaceId)
                || string.IsNullOrWhiteSpace(defaultNLogAzureLogAnalyticsParameters?.AzureAnalyticsSharedSecret)
                || string.IsNullOrWhiteSpace(defaultNLogAzureLogAnalyticsParameters?.AzureAnalyticsLogName))
            {
                app.UseSolhigsonNLogDefaultFileTarget();
                InternalLogger.Error(
                    "Unable to initalize NLog Azure Analytics Target because one or more the the required parameters are missing: " +
                    "[WorkspaceId, Sharedkey or LogName].");
                return app;
            }

            app.ConfigureSolhigsonNLogDefaults();
            var customTarget = new AzureLogAnalyticsTarget(defaultNLogAzureLogAnalyticsParameters.AzureAnalyticsWorkspaceId, 
                defaultNLogAzureLogAnalyticsParameters.AzureAnalyticsSharedSecret, defaultNLogAzureLogAnalyticsParameters.AzureAnalyticsLogName,
                app.ApplicationServices.GetRequiredService<IHttpClientFactory>())
            {
                Name = "custom document",
                Layout = NLogDefaults.GetDefaultJsonLayout(),
            };

            app.UseSolhigsonNLogCustomTarget(new CustomNLogTargetParameters(customTarget));
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
                        .Warn($"Delaying for {timespan.TotalMilliseconds}ms, then making retry {retryAttempt}.");
                });

            services.AddHttpClient(ApiRequestService.DefaultNamedHttpClient)
                .AddTransientHttpErrorPolicy(ConfigurePolicy);

            services.AddHttpClient(AzureLogAnalyticsService.AzureLogAnalyticsNamedHttpClient)
                .AddTransientHttpErrorPolicy(ConfigurePolicy);

            return services;
        }

        private static IServiceCollection AddSolhigsonIdentityManager<TUser, TRole, TRoleGroup, TKey, TContext>(this IServiceCollection services,
            Action<IdentityOptions> setupAction = null) 
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

        #region Logging Extensions

        public static void EServiceStatus(this object obj, string serviceName, string serviceDescription,
            string serviceType,
            bool isUp, string endPointUrl, object data = null,
            string userEmail = null, Exception exception = null)
        {
            if (exception != null)
            {
                isUp = false;
            }

            var desc = string.IsNullOrWhiteSpace(serviceDescription)
                ? "Outbound"
                : serviceDescription;

            var status = isUp ? Constants.ServiceStatus.Up : Constants.ServiceStatus.Down;
            LogManager.GetLogger(obj)?.Log(desc, LogLevel.Info, data, exception, serviceName, serviceType,
                Constants.Group.ServiceStatus, status, endPointUrl, userEmail);
        }

        public static void ELogTrace(this object obj, string message, object data = null, string userEmail = null)
        {
            LogManager.GetLogger(obj)?.Trace(message, data, userEmail);
        }

        public static void ELogDebug(this object obj, string message, object data = null, string userEmail = null)
        {
            LogManager.GetLogger(obj)?.Debug(message, data, userEmail);
        }

        public static void ELogInfo(this object obj, string message, object data = null, string userEmail = null)
        {
            LogManager.GetLogger(obj)?.Info(message, data, userEmail);
        }

        public static void ELogWarn(this object obj, string message, object data = null, string userEmail = null)
        {
            LogManager.GetLogger(obj)?.Warn(message, data, userEmail);
        }

        public static void ELogError(this object obj, string message, object data = null, string userEmail = null)
        {
            LogManager.GetLogger(obj)?.Error(null, message, data, userEmail);
        }

        public static void ELogError(this object obj, Exception e, string message = null, object data = null,
            string userEmail = null)
        {
            LogManager.GetLogger(obj)?.Error(e, message, data, userEmail);
        }

        public static void ELogFatal(this object obj, string message, Exception e = null, object data = null,
            string userEmail = null)
        {
            LogManager.GetLogger(obj)?.Fatal(message, e, data, userEmail);
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



        public static ClaimsPrincipal GetPrincipal(string jwtTokenString, string secret,
            TokenValidationParameters validationParameters,
            string issuer, string audience = null)
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
                Logger.Error(e);
            }

            return null;
        }

        #endregion

        #region EntityFramework Data Extensions (Caching & Paging)

        public static bool IsDbSetType(this Type type)
        {
            if (type is null)
            {
                return false;
            }
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DbSet<>);
        }
        public static string GetCacheKey(this IQueryable query, bool hash = true)
        {
            var expression = query.Expression;

            // locally evaluate as much of the query as possible
            expression = Evaluator.PartialEval(expression, Evaluator.CanBeEvaluatedLocallyFunc);

            // support local collections
            expression = LocalCollectionExpander.Rewrite(expression);

            // use the string representation of the expression for the cache key
            var key = $"{query.ElementType}{expression}";

            if (hash)
            {
                key = key.ToSha256();
            }

            return key;
        }

        public static ResponseInfo<object> GetCacheStatus<T>(this IQueryable<T> query, params Type [] iCachedEntityType) where T : class
        {
            var response = new ResponseInfo<object>();
            var types = GetQueryBaseType(query, iCachedEntityType);
            var queryExpression = query.GetCacheKey(false);
            var data = new
            {
                Type = $"{CacheManager.Flatten(types.Select(t => $"{t.Namespace}.{t.Name}"))}",
                CacheKey = queryExpression.ToSha256(),
                QueryExpression = queryExpression,
            };
            var validTypes = CacheManager.GetValidICacheEntityTypes(iCachedEntityType);
            return !validTypes.Any()
                ? response.Fail($"{CacheManager.Flatten(types.Select(t => t.Name))} does not Inherit from ICacheEntity", result: data) 
                : response.Success(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="iCachedEntityTypesToMonitor">The entity types to monitor for database changes</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> FromCacheList<T>(this IQueryable<T> query, params Type [] iCachedEntityTypesToMonitor) where T : class
        {
            return GetCacheData<T, List<T>>(query, ResolveToList, iCachedEntityTypesToMonitor) ?? new List<T>();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="iCachedEntityTypesToMonitor">The entity types to monitor for database changes</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T FromCacheSingle<T>(this IQueryable<T> query, params Type [] iCachedEntityTypesToMonitor) where T : class
        {
            return GetCacheData<T, T>(query, ResolveToSingle, iCachedEntityTypesToMonitor);
        }

        public static void AddCustomResultToCache<T>(this IQueryable<T> query, object result, params Type [] types) where T : class
        {
            CacheManager.AddToCache(query.GetCacheKey(), result, GetQueryBaseType(query, types));
        }
        
        public static T GetCustomResultFromCache<T>(this IQueryable query) where T : class
        {
            var result = CacheManager.GetFromCache(query.GetCacheKey());
            if (result == null)
            {
                return null;
            }
            Logger.Debug($"Retrieved {query.ElementType.Name} [{query.GetCacheKey(false)}] data from cache");
            return result.Value as T;
        }


        private static TK GetCacheData<T, TK>(IQueryable<T> query, Func<IQueryable<T>, object> func, params Type [] iCachedEntityType)
            where TK : class where T : class
        {
            var key = query.GetCacheKey();
            var customCacheEntry = CacheManager.GetFromCache(key);
            if (customCacheEntry != null)
            {
                if (Logger.IsDebugEnabled)
                {
                    Logger.Debug($"Retrieved {query.ElementType.Name} [{query.GetCacheKey(false)}] data from cache");
                }
                return customCacheEntry.Value as TK;
            }

            if (Logger.IsDebugEnabled)
            {
                Logger.Debug($"Fetching {query.ElementType.Name} [{query.GetCacheKey(false)}] data from db");
            }
            lock (key)
            {
                customCacheEntry = CacheManager.GetFromCache(key);
                if (customCacheEntry != null)
                {
                    return customCacheEntry.Value as TK;
                }

                var result = func(query) as TK;
                
                CacheManager.AddToCache(key, result, GetQueryBaseType(query, iCachedEntityType));
                
                return result;
            }
        }

        private static IEnumerable<Type> GetQueryBaseType<T>(IQueryable<T> query, params Type [] iCachedEntityTypes) where T : class
        {
            var types = new List<Type>();
            if (iCachedEntityTypes != null && iCachedEntityTypes.Any())
            {
                return CacheManager.GetValidICacheEntityTypes(iCachedEntityTypes);
            }
            var type = typeof(T);
            try
            {
                if (query.Expression is System.Linq.Expressions.MethodCallExpression me)
                {
                    if (me.Arguments.Count > 0 && me.Arguments[0].Type.GenericTypeArguments?.Length > 0)
                    {
                        type = me.Arguments[0].Type.GenericTypeArguments[0];
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            types.Add(type);
            return types;
        }

        private static object ResolveToList<T>(IQueryable<T> query) where T : class
        {
            var result = query.AsNoTrackingWithIdentityResolution().ToList();
            return result.Any() ? result : null;
        }

        private static object ResolveToSingle<T>(IQueryable<T> query) where T : class
        {
            return query.AsNoTrackingWithIdentityResolution().FirstOrDefault();
        }

        public static async Task<PagedList<T>> ToPagedListAsync<T>(this IQueryable<T> source, int pageNumber, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return PagedList.Create(items, count, pageNumber, pageSize);
        }

        public static IQueryable<T> DateRangeQuery<T>(this IQueryable<T> source, DateTime fromDate, DateTime toDate)
        where T: IDateSearchable
        {
            return source.Where(t => t.Date >= fromDate && t.Date <= toDate);
        }

        #endregion

        #region Misc

        public static string ToSha256(this string s)
        {
            var bytes = Encoding.Unicode.GetBytes(s.ToCharArray());
            var hash = new SHA256Managed().ComputeHash(bytes);

            // concat the hash bytes into one long string
            return hash.Aggregate(new StringBuilder(32),
                    (sb, b) => sb.Append(b.ToString("X2")))
                .ToString();
        }

        public static string ToConcatenatedString<T>(this IEnumerable<T> source, Func<T, string> selector,
            string separator)
        {
            var b = new StringBuilder();
            bool needSeparator = false;

            foreach (var item in source)
            {
                if (needSeparator)
                    b.Append(separator);

                b.Append(selector(item));
                needSeparator = true;
            }

            return b.ToString();
        }

        public static LinkedList<T> ToLinkedList<T>(this IEnumerable<T> source)
        {
            return new LinkedList<T>(source);
        }

        public static string CallerIp(this HttpContext httpContext)
        {
            return HelperFunctions.GetCallerIp(httpContext);
        }

        #endregion
        
        #region Attributes 
        
        public static T GetAttribute<T>(this Type type, bool includeBaseTypes = true) where T : Attribute
        {
            return type?.GetCustomAttributes<T>(includeBaseTypes).FirstOrDefault();
        }
        
        public static T GetAttribute<T>(this ParameterInfo parameterInfo, bool includeBaseTypes = true) where T : Attribute
        {
            return parameterInfo?.GetCustomAttributes<T>(includeBaseTypes).FirstOrDefault();
        }

        public static T GetAttribute<T>(this MethodInfo methodInfo, bool includeBaseTypes = true) where T : Attribute
        {
            return methodInfo?.GetCustomAttributes<T>(includeBaseTypes).FirstOrDefault();
        }

        public static T GetAttribute<T>(this PropertyInfo propertyInfo, bool includeBaseTypes = true) where T : Attribute
        {
            return propertyInfo?.GetCustomAttributes<T>(includeBaseTypes).FirstOrDefault();
        }


        public static bool HasAttribute<T>(this Type type, bool includeInheritance = true) where T : Attribute
        {
            return type.GetAttribute<T>(includeInheritance) != null;
        }
        
        public static bool HasAttribute<T>(this ParameterInfo type, bool includeInheritance = true) where T : Attribute
        {
            return type.GetAttribute<T>(includeInheritance) != null;
        }
        
        public static bool HasAttribute<T>(this MethodInfo type, bool includeInheritance = true) where T : Attribute
        {
            return type.GetAttribute<T>(includeInheritance) != null;
        }

        public static bool HasAttribute<T>(this PropertyInfo type, bool includeInheritance = true) where T : Attribute
        {
            return type.GetAttribute<T>(includeInheritance) != null;
        }
        
        #endregion
        
        #region String
        
        public static string ToCamelCase(this string str) =>
            string.IsNullOrEmpty(str) || str.Length < 2
                ? str
                : char.ToLowerInvariant(str[0]) + str[1..];

        public static bool IsValidEmailAddress(this string email, bool ignoreEmpty = false)
        {
            return HelperFunctions.IsValidEmailAddress(email, ignoreEmpty);
        }
        
        public static bool IsValidPhoneNumber(this string phoneNumber, bool ignoreEmpty = false)
        {
            return HelperFunctions.IsValidPhoneNumber(phoneNumber, ignoreEmpty);
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
            return new OkObjectResult(response);
        }

        public static bool IsApiController(this HttpContext context)
        {
            return context.GetEndpoint()?.Metadata
                .GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo
                .GetCustomAttribute<ApiControllerAttribute>() != null;
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

        private const string ChainIdKey = "::ApiTrace::ChainId::Key";
        public static string GetChainId(this HttpContext httpContext)
        {
            object chainIdObj = null;
            try
            {
                if (httpContext != null)
                {
                    httpContext.Items?.TryGetValue(ChainIdKey, out chainIdObj);
                }
                else
                {
                    chainIdObj = Thread.GetData(Thread.GetNamedDataSlot(ChainIdKey));
                }
            }
            catch(Exception e)
            {
                Logger.Error(e);
            }

            if (chainIdObj == null)
            {
                return null;
            }
            var cId = Convert.ToString(chainIdObj);
            return !string.IsNullOrWhiteSpace(cId) ? cId : null;
        }

        public static void AddChainId(this HttpContext httpContext, string value)
        {
            try
            {
                if (httpContext != null)
                {
                    httpContext.Items?.TryAdd(ChainIdKey, value);
                }
                else
                {
                    Thread.SetData(Thread.GetNamedDataSlot(ChainIdKey), value);
                }
            }
            catch(Exception e)
            {
                Logger.Error(e);
            }
        }

        
        /*
        public static bool IsPermissionAllowed(this IRazorPage view, string permission)
        {
            return GetController(view)?.IsPermissionAllowed(permission) == true;
        }

    */
    }
}
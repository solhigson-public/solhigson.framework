using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using Solhigson.Framework.Data;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Logging.Dto;
using Solhigson.Framework.Logging.Nlog;
using Solhigson.Framework.Logging.Nlog.Renderers;
using Solhigson.Framework.Logging.Nlog.Targets;
using Solhigson.Framework.Utilities.Linq;
using Solhigson.Framework.Web.Middleware;
using LogLevel = NLog.LogLevel;

namespace Solhigson.Framework.Infrastructure
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

        public static ContainerBuilder RegisterSolhigsonDependencies(this ContainerBuilder builder)
        {
            builder.RegisterModule(new SolhigsonAutofacModule());
            return builder;
        }
        
        public static IApplicationBuilder UseSolhigsonCacheManager(this IApplicationBuilder app, string connectionString, int cacheDependencyChangeTrackerTimerIntervalMilliseconds = 5000,
            int cacheExpirationPeriodMinutes = 1440, Assembly databaseModelsAssembly = null)
        {
            CacheManager.Initialize(connectionString, cacheDependencyChangeTrackerTimerIntervalMilliseconds, cacheExpirationPeriodMinutes, databaseModelsAssembly);
            return app;
        }

        public static IApplicationBuilder UseSolhigsonNLogDefaultFileTarget(this IApplicationBuilder app,
            DefaultNLogParameters defaultNLogParameters = null, IHttpContextAccessor httpContextAccessor = null)
        {
            defaultNLogParameters ??= new DefaultNLogParameters();
            if (defaultNLogParameters.LogApiTrace)
            {
                app.UseMiddleware<SolhigsonApiTraceMiddleware>();
            }
            ConfigurationItemFactory.Default.CreateInstance = type =>
                type == typeof(CustomDataRenderer)
                    ? new CustomDataRenderer(defaultNLogParameters.ProtectedFields)
                    : Activator.CreateInstance(type);
            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget
            {
                FileName = $"{Environment.CurrentDirectory}/log.log",
                Name = "FileDefault",
                ArchiveAboveSize = 2560000,
                ArchiveNumbering = ArchiveNumberingMode.Sequence,
                Layout = DefaultLayout.Layout
            };
            config.AddRule(LogLevel.Info, LogLevel.Error, fileTarget);
            NLog.LogManager.Configuration = config;
            LogManager.SetLogLevel(defaultNLogParameters.LogLevel);
            LogManager.HttpContextAccessor = httpContextAccessor;
            return app;
        }

        public static IApplicationBuilder UseSolhigsonNLogAzureLogAnalyticsTarget(this IApplicationBuilder app,
            DefaultNLogAzureLogAnalyticsTarget defaultNLogAzureLogAnalyticsTarget = null)
        {
            defaultNLogAzureLogAnalyticsTarget ??= new DefaultNLogAzureLogAnalyticsTarget();
            app.UseSolhigsonNLogDefaultFileTarget(defaultNLogAzureLogAnalyticsTarget);
            
            if (string.IsNullOrWhiteSpace(defaultNLogAzureLogAnalyticsTarget.AzureAnalyticsWorkspaceId)
                || string.IsNullOrWhiteSpace(defaultNLogAzureLogAnalyticsTarget.AzureAnalyticsSharedSecret)
                || string.IsNullOrWhiteSpace(defaultNLogAzureLogAnalyticsTarget.AzureAnalyticsLogName))
            {
                InternalLogger.Error(
                    "Unable to initalize NLog Azure Analytics Target because one or more the the required parameters are missing: " +
                    "[WorkspaceId, Sharedkey or LogName].");
                return app;
            }

            var config = new LoggingConfiguration();
            var fallbackGroupTarget = new FallbackGroupTarget
            {
                Name = "FallBackTargets",
            };
            var fileFallbackTarget = new FileTarget
            {
                FileName = $"{Environment.CurrentDirectory}/log.log",
                Name = "FileFallback",
                ArchiveAboveSize = 2560000,
                ArchiveNumbering = ArchiveNumberingMode.Sequence,
                Layout = DefaultLayout.Layout
            };

            var customTarget = new AzureLogAnalyticsTarget(defaultNLogAzureLogAnalyticsTarget.AzureAnalyticsWorkspaceId, 
                defaultNLogAzureLogAnalyticsTarget.AzureAnalyticsSharedSecret, defaultNLogAzureLogAnalyticsTarget.AzureAnalyticsLogName)
            {
                Name = "custom document",
                Layout = DefaultLayout.Layout,
            };

            fallbackGroupTarget.Targets.Add(customTarget);
            fallbackGroupTarget.Targets.Add(fileFallbackTarget);
            config.AddRule(LogLevel.Info, LogLevel.Error, fallbackGroupTarget);

            NLog.LogManager.Configuration = config;

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

        #region EntityFramework Data Extensions

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


        public static IList<T> FromCacheCollection<T>(this IQueryable<T> query) where T : class, ICachedEntity
        {
            return GetCacheData<T, List<T>>(query, ResolveToList);
        }

        public static T FromCacheSingle<T>(this IQueryable<T> query) where T : class, ICachedEntity
        {
            return GetCacheData<T, T>(query, ResolveToSingle);
        }

        private static TK GetCacheData<T, TK>(IQueryable<T> query, Func<IQueryable<T>, object> func)
            where TK : class where T : class
        {
            var key = query.GetCacheKey();
            var data = CacheManager.GetFromCache<TK>(key);
            if (data != null)
            {
                Logger.Debug($"Retrieved {query.ElementType.Name} [{query.GetCacheKey(false)}] data from cache");
                return data;
            }

            Logger.Debug($"Fetching {query.ElementType.Name} [{query.GetCacheKey(false)}] data from db");
            lock (key)
            {
                data = CacheManager.GetFromCache<TK>(key);
                if (data != null)
                {
                    return data;
                }

                data = func(query) as TK;
                if (data != null)
                {
                    CacheManager.InsertItem(key, data,
                        new TableChangeMonitor(CacheManager.GetTableChangeTracker(query.ElementType)));
                }

                return data;
            }
        }

        private static object ResolveToList<T>(IQueryable<T> query) where T : class
        {
            return query.AsNoTrackingWithIdentityResolution().ToList();
        }

        private static object ResolveToSingle<T>(IQueryable<T> query) where T : class
        {
            return query.AsNoTrackingWithIdentityResolution().FirstOrDefault();
        }

        public static async Task<PagedList<T>> ToPagedList<T>(this IQueryable<T> source, int pageNumber, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedList<T>(items, count, pageNumber, pageSize);
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

        #endregion
        
        #region Attributes 
        
        public static T GetAttribute<T>(this object obj, bool includeInheritance = true) where T : Attribute
        {
            if (obj is null)
            {
                return null;
            }
            var attributes = obj.GetType().GetCustomAttributes(typeof(T), includeInheritance);

            if (attributes.Length > 0)
            {
                return attributes[0] as T;
            }

            return null;
        }

        public static T GetAttribute<T>(this Type type, bool includeInheritance = true) where T : Attribute
        {
            if (type == null)
            {
                return null;
            }
            var attributes = type.GetCustomAttributes(typeof(T), includeInheritance);

            if (attributes.Length > 0)
            {
                return attributes[0] as T;
            }

            return null;
        }

        public static bool HasAttribute<T>(this Type type, bool includeInheritance = true) where T : Attribute
        {
            return type.GetAttribute<T>(includeInheritance) != null;
        }
        
        public static bool HasAttribute<T>(this object obj, bool includeInheritance = true) where T : Attribute
        {
            return obj.GetAttribute<T>(includeInheritance) != null;
        }
        
        #endregion
    }
}
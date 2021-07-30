using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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
        #region Api Extensions

        public static Dictionary<string, string> AddAuthorizationHeader(this Dictionary<string, string> headers,
            string type, string value)
        {
            headers?.Add("Authorization", $"{type} {value}");
            return headers;
        }

        #endregion

        #region Application Startup

        public static IApplicationBuilder UseSolhigsonCacheManager(this IApplicationBuilder app, string connectionString, int cacheDependencyChangeTrackerTimerIntervalMilliseconds = 5000,
            int cacheExpirationPeriodMinutes = 1440)
        {
            CacheManager.Initialize(connectionString);
            return app;
        }

        public static IApplicationBuilder UseSolhigsonNLogDefaultFileTarget(this IApplicationBuilder app,
            DefaultNLogParameters defaultNLogParameters = null, IHttpContextAccessor httpContextAccessor = null)
        {
            defaultNLogParameters ??= new DefaultNLogParameters();
            if (defaultNLogParameters.LogApiTrace)
            {
                app.UseMiddleware<SolhigsonRequestResponseLogger>();
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

        #region Identity

        public static string GetUserId(this IIdentity identity)
        {
            return GetClaimValue(identity, ClaimTypes.NameIdentifier);
        }

        public static string GetPartnershipToken(this IIdentity identity)
        {
            return GetClaimValue(identity, ClaimTypes.GivenName);
        }

        public static string GetEmail(this IIdentity identity)
        {
            return GetClaimValue(identity, ClaimTypes.Email);
        }

        private static string GetClaimValue(this IEnumerable<Claim> claims, string claimType)
        {
            if (claims == null)
            {
                return null;
            }

            var claimsList = new List<Claim>(claims);
            var claim = claimsList.Find(c => c.Type == claimType);
            return claim?.Value;
        }

        private static string GetClaimValue(this IIdentity identity, string claimType)
        {
            var claimIdentity = (ClaimsIdentity) identity;
            return claimIdentity?.Claims?.GetClaimValue(claimType);
        }

        public static string GetUserEmail(this IHttpContextAccessor httpContextAccessor)
        {
            return httpContextAccessor?.HttpContext?.User?.Identity?.GetClaimValue(ClaimTypes.Email);
        }

        #endregion

        #region EntityFramework Data Extensions

        public static string GetCacheKey(this IQueryable query)
        {
            var expression = query.Expression;

            // locally evaluate as much of the query as possible
            expression = Evaluator.PartialEval(expression, Evaluator.CanBeEvaluatedLocallyFunc);

            // support local collections
            expression = LocalCollectionExpander.Rewrite(expression);

            // use the string representation of the expression for the cache key
            var key = $"{query.ElementType}{expression}";

            // the key is potentially very long, so use an md5 fingerprint
            // (fine if the query result data isn't critically sensitive)
            key = key.ToSha256();

            return key;
        }


        public static string FromCache(this IQueryable query) // where T : ICachedTable
        {
            return $"{query.ElementType}_{query.Expression}";
        }

        public static IList<T> FromCacheCollection<T>(this IQueryable<T> query) where T : class, ICachedData
        {
            return GetCacheData<T, List<T>>(query, ResolveToList);
        }

        public static T FromCacheSingle<T>(this IQueryable<T> query) where T : class, ICachedData
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
                return data;
            }

            lock (key)
            {
                data = CacheManager.GetFromCache<TK>(key);
                if (data != null)
                {
                    return data;
                }

                data = func(query) as TK;
                CacheManager.InsertItem(key, data);
                return data;
            }
        }

        private static object ResolveToList<T>(IQueryable<T> query) where T : class
        {
            return query.ToList();
        }

        private static object ResolveToSingle<T>(IQueryable<T> query) where T : class
        {
            return query.FirstOrDefault();
        }

        internal static void CheckAndUpdateCachedData(this DbContext context)
        {
            if (context == null)
            {
                return;
            }

            var entries = context.ChangeTracker
                .Entries()
                .Where(e => e.Entity is ICachedData && (e.State == EntityState.Added ||
                                                        e.State == EntityState.Modified ||
                                                        e.State == EntityState.Deleted));

            if (entries.Any())
            {
                _ = CacheManager.ResyncCache();
            }
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
    }
}
using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using NLog;
using NLog.Common;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Logging
{
    public static class LogManager
    {
        private static readonly ConcurrentDictionary<string, LogWrapper> LogWrappers =
            new ();

        public static void SetLogLevel(string level)
        {
            level ??= "info";
            GetCurrentClassLogger().Debug($"Setting log level to {level}");

            var logLevel = level.ToLower() switch
            {
                "info" => LogLevel.Info,
                "trace" => LogLevel.Trace,
                "warn" => LogLevel.Warn,
                "debug" => LogLevel.Debug,
                "error" => LogLevel.Error,
                _ => LogLevel.Info
            };

            SetLoggingLevel(logLevel);
        }

        private static void SetLoggingLevel(LogLevel level)
        {
            if (NLog.LogManager.Configuration == null) return;
            // Uncomment these to enable NLog logging. NLog exceptions are swallowed by default.
            ////NLog.Common.InternalLogger.LogFile = @"C:\Temp\nlog.debug.log";
            ////NLog.Common.InternalLogger.LogLevel = LogLevel.Debug;

            if (level == LogLevel.Off)
            {
                NLog.LogManager.DisableLogging();
            }
            else
            {
                if (!NLog.LogManager.IsLoggingEnabled()) NLog.LogManager.EnableLogging();

                foreach (var rule in NLog.LogManager.Configuration.LoggingRules)
                {
                    rule.DisableLoggingForLevels(LogLevel.Trace, LogLevel.Fatal);
                    // Iterate over all levels up to and including the target, (re)enabling them.
                    for (var i = level.Ordinal; i <= 5; i++) rule.EnableLoggingForLevel(LogLevel.FromOrdinal(i));
                }
            }

            NLog.LogManager.ReconfigExistingLoggers();
        }

        public static LogWrapper GetCurrentClassLogger()
        {
            string name = null;
            try
            {
                name = StackTraceUsageUtils.GetClassFullName();
            }
            catch (Exception e)
            {
                InternalLogger.Error(e, e.Message);
            }

            return GetLoggerInternal(name);
        }

        private static LogWrapper GetLoggerInternal(string name, object obj = null)
        {
            if (string.IsNullOrEmpty(name)) name = "MISC";

            LogWrappers.TryGetValue(name, out var logWrapper);

            if (logWrapper != null) return logWrapper;
            logWrapper = new LogWrapper(name);
            LogWrappers.TryAdd(name, logWrapper);
            return logWrapper;
        }

        internal static LogWrapper GetLogger(object obj)
        {
            return obj == null ? null : GetLoggerInternal(obj.GetType().FullName);
        }

        public static LogWrapper GetLogger(string loggerName)
        {
            return GetLoggerInternal(loggerName);
        }
    }
}
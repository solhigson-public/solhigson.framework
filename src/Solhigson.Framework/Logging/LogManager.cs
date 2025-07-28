using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = NLog.LogLevel;

// using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Solhigson.Framework.Logging;

public static class LogManager
{
    private static ILoggerFactory? _loggerFactory;
    private static readonly ConcurrentDictionary<string, LogWrapper> LogWrappers =
        new ();
    private static ILogger? _logger;
    internal static string? ServiceName;

    public static void SetLoggerFactory(ILoggerFactory loggerFactory, string? serviceName = null)
    {   
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger("Solhigson.LogManager");
        ServiceName = serviceName;
    }
    
    public static void SetLogLevel(string? level)
    {
        level ??= "info";

        var logLevel = level.ToLower() switch
        {
            "info" => LogLevel.Info,
            "trace" => LogLevel.Trace,
            "warn" => LogLevel.Warn,
            "debug" => LogLevel.Debug,
            "error" => LogLevel.Error,
            _ => LogLevel.Info
        };

        _logger?.LogDebug("Setting log level to {level}", level);
        SetLoggingLevel(logLevel);
    }

    private static void SetLoggingLevel(LogLevel level)
    {
        if (NLog.LogManager.Configuration == null)
        {
            return;
        }

        if (level == LogLevel.Off)
        {
            NLog.LogManager.SuspendLogging();
        }
        else
        {
            if (!NLog.LogManager.IsLoggingEnabled()) NLog.LogManager.ResumeLogging();

            foreach (var rule in NLog.LogManager.Configuration.LoggingRules)
            {
                rule.DisableLoggingForLevels(LogLevel.Trace, LogLevel.Fatal);
                // Iterate over all levels up to and including the target, (re)enabling them.
                for (var i = level.Ordinal; i <= 5; i++) rule.EnableLoggingForLevel(LogLevel.FromOrdinal(i));
            }
        }

        NLog.LogManager.ReconfigExistingLoggers();
    }

    private static LogWrapper GetLoggerInternal(string? name, ILoggerFactory? factory = null)
    {
        factory ??= _loggerFactory;   
        if (string.IsNullOrEmpty(name)) name = "MISC";

        LogWrappers.TryGetValue(name, out var logWrapper);

        if (logWrapper != null) return logWrapper;
        logWrapper = new LogWrapper(name, factory);
        LogWrappers.TryAdd(name, logWrapper);
        return logWrapper;
    }
    
    internal static LogWrapper GetLogger(object? obj, ILoggerFactory? factory = null)
    {
        factory ??= _loggerFactory;   
        return GetLoggerInternal(obj?.GetType().FullName, factory);
    }
    
    public static LogWrapper GetLogger(string? loggerName, ILoggerFactory? factory = null)
    {
        factory ??= _loggerFactory;   
        return GetLoggerInternal(loggerName, factory);
    }



}
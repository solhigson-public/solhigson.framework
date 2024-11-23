using System;
using Microsoft.Extensions.Logging;
using NLog;
using Solhigson.Framework.Infrastructure;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogManager = Solhigson.Framework.Logging.LogManager;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Solhigson.Framework.Extensions;

public static class LoggerExtensions
{
    private static void Log(object obj, LogLevel level, string? message, Exception? e = null, params object?[]? args)
    {
        LogManager.GetLogger(obj).Log(level, message, e, args);
    }
    
    [Obsolete("This will be depreciated in future releases, use LogTrace() instead")]
    public static void ELogTrace(this object obj, string message, object? data = null)
    {
        Log(obj, LogLevel.Trace, message, null, data);
    }
    
    [MessageTemplateFormatMethod("message")]
    public static void LogTrace(this object obj, string message, params object?[]? args)
    {
        Log(obj, LogLevel.Trace, message, null, args);
    }


    [Obsolete("This will be depreciated in future releases, use LogDebug() instead")]
    public static void ELogDebug(this object obj, string message, object? data = null)
    {
        Log(obj, LogLevel.Debug, message, null, data);
    }
    
    [MessageTemplateFormatMethod("message")]
    public static void LogDebug(this object obj, string message, params object?[]? args)
    {
        Log(obj, LogLevel.Debug, message, null, args);
    }


    [Obsolete("This will be depreciated in future releases, use LogInfo() instead")]
    public static void ELogInfo(this object obj, string message, object? data = null)
    {
        Log(obj, LogLevel.Information, message, null, data);
    }
    
    [MessageTemplateFormatMethod("message")]
    public static void LogInformation(this object obj, string message, params object?[]? args)
    {
        Log(obj, LogLevel.Information, message, null, args);
    }

    [Obsolete("This will be depreciated in future releases, use LogWarning() instead")]
    public static void ELogWarn(this object obj, string message, object? data = null)
    {
        Log(obj, LogLevel.Warning, message, null, data);
    }
    
    [MessageTemplateFormatMethod("message")]
    public static void LogWarning(this object obj, string message, params object?[]? args)
    {
        Log(obj, LogLevel.Warning, message, null, args);
    }


    [Obsolete("This will be depreciated in future releases, use LogError() instead")]
    public static void ELogError(this object obj, string message, object? data = null)
    {
        Log(obj, LogLevel.Error, message, null, data);
    }
    
    [MessageTemplateFormatMethod("message")]
    public static void LogError(this object obj, string message, params object?[]? args)
    {
        Log(obj, LogLevel.Error, message, null, args);
    }


    [Obsolete("This will be depreciated in future releases, use LogError() instead")]
    public static void ELogError(this object obj, Exception e, string? message = null, object? data = null,
        string? userEmail = null)
    {
        Log(obj, LogLevel.Error, message, e, data);
    }
    
    [MessageTemplateFormatMethod("message")]
    public static void LogError(this object obj, Exception e, string? message = null, params object?[]? args)
    {
        Log(obj, LogLevel.Error, message, e, args);
    }


    [Obsolete("This will be depreciated in future releases, use LogCritical() instead")]
    public static void ELogFatal(this object obj, string message, Exception? e = null, object? data = null,
        string? userEmail = null)
    {
        Log(obj, LogLevel.Critical, message, e, data);
    }
    
    [MessageTemplateFormatMethod("message")]
    public static void LogCritical(this object obj, string message, params object?[]? args)
    {
        Log(obj, LogLevel.Error, message, null, args);
    }

    [MessageTemplateFormatMethod("message")]
    public static void LogCritical(this object obj, Exception e, string message, params object?[]? args)
    {
        Log(obj, LogLevel.Critical, message, e, args);
    }
    
    public static void SetCurrentLogChainId(this object obj, string chainId)
    {
        ServiceProviderWrapper.SetCurrentLogChainId(chainId);
    }

    public static string? GetCurrentLogChainId(this object obj)
    {
        return ServiceProviderWrapper.GetCurrentLogChainId();
    }

    public static void SetCurrentLogUserEmail(this object obj, string email)
    {
        ServiceProviderWrapper.SetCurrentLogUserEmail(email);
    }

    // private static LogWrapper GetLoggerInternal(string name)
    // {
    //     if (string.IsNullOrEmpty(name)) name = "MISC";
    //
    //     LogWrappers.TryGetValue(name, out var logWrapper);
    //
    //     if (logWrapper != null) return logWrapper;
    //     logWrapper = new LogWrapper(name);
    //     LogWrappers.TryAdd(name, logWrapper);
    //     return logWrapper;
    // }
    //
    // internal static LogWrapper GetLogger(object obj)
    // {
    //     return obj == null ? null : GetLoggerInternal(obj.GetType().FullName);
    // }
}
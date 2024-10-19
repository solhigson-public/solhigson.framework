using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NLog;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;
using ILogger = NLog.ILogger;
using LogLevel = NLog.LogLevel;
using LogManager = Solhigson.Framework.Logging.LogManager;

namespace Solhigson.Framework.Extensions;

public static class LoggerExtensions
{
    private static readonly Logger LoggerInternal = NLog.LogManager.GetLogger(nameof(LoggerExtensions));
    public static void EServiceStatus(this object obj, string serviceName, string serviceDescription,
        string serviceType,
        bool isUp, string endPointUrl, object data = null,
        string userEmail = null, Exception exception = null, string chainId = null)
    {
        if (!LoggerInternal.IsInfoEnabled)
        {
            return;
        }

        if (exception != null)
        {
            isUp = false;
        }

        var desc = string.IsNullOrWhiteSpace(serviceDescription)
            ? "Outbound"
            : serviceDescription;

        var status = isUp ? Constants.ServiceStatus.Up : Constants.ServiceStatus.Down;
        LogManager.GetLogger(obj)?.Log(desc, LogLevel.Info, data, exception, serviceName, serviceType,
            Constants.Group.ServiceStatus, status, endPointUrl, chainId);
    }

    [Obsolete("This will be depreciated in future releases, use LogTrace() instead")]
    public static void ELogTrace(this object obj, string message, object data = null)
    {
        if (LoggerInternal.IsTraceEnabled)
        {
            LogManager.GetLogger(obj)?.Trace(message, data);
        }
    }
    
    [MessageTemplateFormatMethod("message")]
    public static void LogTrace(this object obj, string message, params object?[] args)
    {
        if (LoggerInternal.IsTraceEnabled)
        {
            LogManager.GetLogger(obj)?.LogTrace(message, args);
        }
    }


    [Obsolete("This will be depreciated in future releases, use LogDebug() instead")]
    public static void ELogDebug(this object obj, string message, object data = null)
    {
        if (LoggerInternal.IsDebugEnabled)
        {
            LogManager.GetLogger(obj)?.Debug(message, data);
        }
    }
    
    [MessageTemplateFormatMethod("message")]
    public static void LogDebug(this object obj, string message, params object[] args)
    {
        if (LoggerInternal.IsDebugEnabled)
        {
            LogManager.GetLogger(obj)?.LogDebug(message, args);
        }
    }


    [Obsolete("This will be depreciated in future releases, use LogInfo() instead")]
    public static void ELogInfo(this object obj, string message, object data = null)
    {
        if (LoggerInternal.IsInfoEnabled)
        {
            LogManager.GetLogger(obj)?.Info(message, data);
        }
    }
    
    [MessageTemplateFormatMethod("message")]
    public static void LogInformation(this object obj, string message, params object[] args)
    {
        if (LoggerInternal.IsInfoEnabled)
        {
            LogManager.GetLogger(obj)?.LogInformation(message, args);
        }
    }

    [Obsolete("This will be depreciated in future releases, use LogWarn() instead")]
    public static void ELogWarn(this object obj, string message, object data = null)
    {
        if (LoggerInternal.IsWarnEnabled)
        {
            LogManager.GetLogger(obj)?.Warn(message, data);
        }
    }
    
    [MessageTemplateFormatMethod("message")]
    public static void LogWarn(this object obj, string message, params object[] args)
    {
        if (LoggerInternal.IsWarnEnabled)
        {
            LogManager.GetLogger(obj)?.LogWarn(message, args);
        }
    }


    [Obsolete("This will be depreciated in future releases, use LogError() instead")]
    public static void ELogError(this object obj, string message, object data = null)
    {
        if (LoggerInternal.IsErrorEnabled)
        {
            LogManager.GetLogger(obj)?.Error(null, message, data);
        }
    }
    
    [MessageTemplateFormatMethod("message")]
    public static void LogError(this object obj, string message, params object[] args)
    {
        if (LoggerInternal.IsErrorEnabled)
        {
            LogManager.GetLogger(obj)?.LogError(null, message, args);
        }
    }


    [Obsolete("This will be depreciated in future releases, use LogError() instead")]
    public static void ELogError(this object obj, Exception e, string message = null, object data = null,
        string userEmail = null)
    {
        if (LoggerInternal.IsErrorEnabled)
        {
            LogManager.GetLogger(obj)?.Error(e, message ?? e.Message, data);
        }
    }
    
    [MessageTemplateFormatMethod("message")]
    public static void LogError(this object obj, Exception e, string message = null, params object?[] args)
    {
        if (LoggerInternal.IsErrorEnabled)
        {
            LogManager.GetLogger(obj)?.LogError(e, message, args);
        }
    }


    [Obsolete("This will be depreciated in future releases, use LogFatal() instead")]
    public static void ELogFatal(this object obj, string message, Exception e = null, object data = null,
        string userEmail = null)
    {
        if (LoggerInternal.IsFatalEnabled)
        {
            LogManager.GetLogger(obj)?.Fatal(message, e, data);
        }
    }
    
    [MessageTemplateFormatMethod("message")]
    public static void LogFatal(this object obj, string message, params object[] args)
    {
        if (LoggerInternal.IsFatalEnabled)
        {
            LogManager.GetLogger(obj)?.LogFatal(null, message, args);
        }
    }

    public static Logger Logger(this object obj) => LogManager.GetLogger(obj)?.InternalLogger;

    

    public static void SetCurrentLogChainId(this object obj, string chainId)
    {
        ServiceProviderWrapper.SetCurrentLogChainId(chainId);
    }

    public static string GetCurrentLogChainId(this object obj)
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
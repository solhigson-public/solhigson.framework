using System;
using System.Collections.Concurrent;
using NLog;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;
using LogLevel = NLog.LogLevel;
using LogManager = Solhigson.Framework.Logging.LogManager;

namespace Solhigson.Framework.Extensions;

public static class LoggerExtensions
{
    private static readonly Logger Logger = NLog.LogManager.GetLogger(nameof(LoggerExtensions));
    private static readonly ConcurrentDictionary<string, LogWrapper> LogWrappers =
        new ();
    
    /*
    private const string tempName = "Solhigson.Framework.Benchmarks.Whiteboard.Benchmark";
    static LogManager()
    {
        LogWrappers.TryAdd(tempName, LogManager.GetLogger(tempName));

    }
    */

    #region Logging Extensions

    public static void EServiceStatus(this object obj, string serviceName, string serviceDescription,
        string serviceType,
        bool isUp, string endPointUrl, object data = null,
        string userEmail = null, Exception exception = null)
    {
        if (Logger.IsInfoEnabled)
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
            Constants.Group.ServiceStatus, status, endPointUrl, userEmail);
    }

    public static void ELogTrace(this object obj, string message, object data = null, string userEmail = null)
    {
        if (Logger.IsTraceEnabled)
        {
            LogManager.GetLogger(obj)?.Trace(message, data, userEmail);
        }
    }

    
    /*
    public static void ELogDebug(this object obj, FormattableString message, object data = null, string userEmail = null)
    {
        //LogManager.Log(logger, message, LogLevel.Info, data, userEmail: userEmail);
        if (LogManager.IsLoggerEnabled(LogLevel.Debug))
        {
            LogManager.GetLogger(obj, LogLevel.Debug)?.Debug(message.ToString(), data, userEmail);
        }
    }
    */

    public static void ELogDebug(this object obj, string message, object data = null, string userEmail = null)
    {
        //LogManager.Log(logger, message, LogLevel.Info, data, userEmail: userEmail);
        if (Logger.IsDebugEnabled)
        {
            GetLogger(obj)?.Debug(message, data, userEmail);
        }
    }


    public static void ELogInfo(this object obj, string message, object data = null, string userEmail = null)
    {
        if (Logger.IsInfoEnabled)
        {
            LogManager.GetLogger(obj)?.Info(message, data, userEmail);
        }
    }

    public static void ELogWarn(this object obj, string message, object data = null, string userEmail = null)
    {
        if (Logger.IsWarnEnabled)
        {
            LogManager.GetLogger(obj)?.Warn(message, data, userEmail);
        }
    }

    public static void ELogError(this object obj, string message, object data = null, string userEmail = null)
    {
        if (Logger.IsErrorEnabled)
        {
            LogManager.GetLogger(obj)?.Error(null, message, data, userEmail);
        }
    }

    public static void ELogError(this object obj, Exception e, string message = null, object data = null,
        string userEmail = null)
    {
        if (Logger.IsErrorEnabled)
        {
            LogManager.GetLogger(obj)?.Error(e, message, data, userEmail);
        }
    }

    public static void ELogFatal(this object obj, string message, Exception e = null, object data = null,
        string userEmail = null)
    {
        if (Logger.IsFatalEnabled)
        {
            LogManager.GetLogger(obj)?.Fatal(message, e, data, userEmail);
        }
    }

    #endregion

    private static LogWrapper GetLoggerInternal(string name)
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

}
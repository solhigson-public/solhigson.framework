using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging.Nlog.Renderers;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using NLogLevel = NLog.LogLevel;

namespace Solhigson.Framework.Logging;

public class LogWrapper
{
    internal LogWrapper(string name, ILoggerFactory? loggerFactory)
    {
        InternalLogger = loggerFactory?.CreateLogger(name);
        InternalLogger2 = NLog.LogManager.GetLogger(name);
    }

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        return InternalLogger is not null && InternalLogger.IsEnabled(logLevel);
    }

    public ILogger? InternalLogger { get; }
    public Logger InternalLogger2 { get; }

    internal void Log(LogLevel logLevel, string? message, params object?[]? args)
    {
        Log(logLevel, message, null, args);
    }    
    
    internal void Log(LogLevel logLevel, string? message, Exception? exception, params object?[]? args)
    {
        if (InternalLogger is null)
        {
            return;
        }
        try
        {
            if (exception is TaskCanceledException or OperationCanceledException)
            {
                var configurationWrapper = ServiceProviderWrapper.ServiceProvider?.GetService<ConfigurationWrapper>();
                if (configurationWrapper is not null)
                {
                    if (configurationWrapper.GetConfigAsync<bool>("appSettings", "IgnoreTaskCancelledException", "false").Result)
                    {
                        return;
                    }
                }
            }
        }
        catch
        {
            //
        }

        LogEventInfo eventInfo;
        if (!args.HasData())
        {
            eventInfo = LogEventInfo.Create(GetNLogLevel(logLevel), InternalLogger2.Name, exception, CultureInfo.InvariantCulture,
                message, args);
        }
        else
        {
            eventInfo = LogEventInfo.Create(GetNLogLevel(logLevel), InternalLogger2.Name, exception, CultureInfo.InvariantCulture,
                message);
        }
        eventInfo.Properties[UserRenderer.Name] = ServiceProviderWrapper.GetHttpContextAccessor()?.GetEmailClaim() ??
                                                  ServiceProviderWrapper.GetCurrentLogUserEmail();
        eventInfo.Properties["chainId"] = ServiceProviderWrapper.GetCurrentLogChainId();
     
        InternalLogger2.Log(eventInfo);
        // if (!string.IsNullOrWhiteSpace(email) || !string.IsNullOrEmpty(chainId))
        // {
        //     var dic = new Dictionary<string, object?>();
        //     if (!string.IsNullOrEmpty(email))
        //     {
        //         dic.Add("Email", email);
        //     }
        //
        //     if (!string.IsNullOrEmpty(chainId))
        //     {
        //         dic.Add("ChainId", chainId);
        //     }
        //     using var scope = InternalLogger.BeginScope(dic);
        //     LogInternal(InternalLogger, logLevel, message, exception, args);
        // }
        // else
        // {   
        //     LogInternal(InternalLogger, logLevel, message, exception, args);
        // }
    }

    private void LogInternal(ILogger logger, LogLevel logLevel, string? message, Exception? exception, params object?[]? args)
    {
        LogEventInfo eventInfo;
        if (!args.HasData())
        {
            eventInfo = LogEventInfo.Create(GetNLogLevel(logLevel), InternalLogger2.Name, exception, CultureInfo.InvariantCulture,
                message, args);
            //logger.Log(logLevel, exception, message);
        }
        else
        {
            eventInfo = LogEventInfo.Create(GetNLogLevel(logLevel), InternalLogger2.Name, exception, CultureInfo.InvariantCulture,
                message);
            //logger.Log(logLevel, exception, message, args!);
        }
        InternalLogger2.Log(eventInfo);
    }

    private static NLogLevel GetNLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => NLogLevel.Trace,
            LogLevel.Critical => NLogLevel.Fatal,
            LogLevel.Debug => NLogLevel.Debug,
            LogLevel.Error => NLogLevel.Error,
            LogLevel.Information => NLogLevel.Info,
            LogLevel.Warning => NLogLevel.Warn,
        };
    }
    
    [MessageTemplateFormatMethod("message")]
    public void LogDebug(string message, params object?[]? args)
    {
        Log(LogLevel.Debug, message, args);
    }

    [MessageTemplateFormatMethod("message")]
    public void LogInformation(string message, params object?[]? args)
    {
        Log(LogLevel.Information, message, args);
    }
    
    [MessageTemplateFormatMethod("message")]
    public void LogWarning(string message, params object?[]? args)
    {
        Log(LogLevel.Warning, message, args);
    }

    [MessageTemplateFormatMethod("message")]
    public void LogTrace(string message, params object?[]? args)
    {
        Log(LogLevel.Trace, message, args);
    }

    [MessageTemplateFormatMethod("message")]
    public void LogError(Exception e, string? message = null, params object?[]? args)
    {
        Log(LogLevel.Error, message, e, args);
    }
    
    [MessageTemplateFormatMethod("message")]
    public void LogCritical(Exception e, string? message = null, params object?[]? args)
    {
        Log(LogLevel.Critical, message, e, args);
    }


}


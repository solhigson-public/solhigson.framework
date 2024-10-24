using System;
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

namespace Solhigson.Framework.Logging;

public class LogWrapper
{
    internal LogWrapper(string name, ILoggerFactory? loggerFactory)
    {
        InternalLogger = loggerFactory?.CreateLogger(name);
    }

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        if (InternalLogger is null)
        {
            return false;
        }
        return InternalLogger.IsEnabled(logLevel);
    }

    public ILogger? InternalLogger { get; }

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
                    if (configurationWrapper.GetConfig<bool>("appSettings", "IgnoreTaskCancelledException", "false"))
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

        var name = ServiceProviderWrapper.GetHttpContextAccessor()?.GetEmailClaim() ??
                                                  ServiceProviderWrapper.GetCurrentLogUserEmail();
        var chainId = ServiceProviderWrapper.GetCurrentLogChainId();

        if (args is null)
        {
            InternalLogger.Log(logLevel, exception, message);
        }
        else
        {
            InternalLogger.Log(logLevel, exception, message, args);

        }
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


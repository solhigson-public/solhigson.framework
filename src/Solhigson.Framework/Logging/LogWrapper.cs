using System;
using System.Threading.Tasks;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using NLogLevel = NLog.LogLevel;

namespace Solhigson.Framework.Logging;

public class LogWrapper
{
    private readonly ILogger? _logger;
    internal LogWrapper(string name, ILoggerFactory? loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger(name);
    }

    private void Log(LogLevel logLevel, string? message, params object?[]? args)
    {
        Log(logLevel, message, null, args);
    }

    internal void Log(LogLevel logLevel, string? message, Exception? exception, params object?[]? args)
    {
        try
        {
            LogInternal(logLevel, message, exception, args);
        }
        catch (Exception e)
        {
            if (args.HasData())
            {
                NLog.Common.InternalLogger.Log(e, GetNLogLevel(logLevel), $"Trying to log {message}", args!);
            }
            else
            {
                NLog.Common.InternalLogger.Log(e, GetNLogLevel(logLevel), $"Trying to log {message}");
            }
        }
    }

    private void LogInternal(LogLevel logLevel, string? message, Exception? exception, params object?[]? args)
    {
        if (_logger is null)
        {
            NLog.Common.InternalLogger.Log(exception, GetNLogLevel(logLevel), message, args);
            return;
        }
        
        if (!_logger.IsEnabled(logLevel))
        {
            return;
        }
        
        // var email = ServiceProviderWrapper.GetHttpContextAccessor()?.GetEmailClaim() ??
        //             ServiceProviderWrapper.GetCurrentLogUserEmail();
        // var chainId = ServiceProviderWrapper.GetCurrentLogChainId();
        //
        // object?[]? otherArgs = null;
        // if (!string.IsNullOrWhiteSpace(email) || !string.IsNullOrEmpty(chainId))
        // {
        //     switch (string.IsNullOrWhiteSpace(email))
        //     {
        //         case false when !string.IsNullOrWhiteSpace(chainId):
        //             otherArgs = [email, chainId];
        //             message += " |Email: {Email} |ChainId: {ChainId}";
        //             break;
        //         case false:
        //             otherArgs = [email];
        //             message += " |Email: {Email}";
        //             break;
        //         default:
        //             otherArgs = [chainId];
        //             message += " |ChainId: {ChainId}";
        //             break;
        //     }
        // }
        Log(_logger, logLevel, message, exception, null, args);
    }

    private static void Log(ILogger logger, LogLevel logLevel, string? message, Exception? exception,
        object?[]? otherArgs, params object?[]? args)
    {
        if (exception is not null)
        {
            message = $"{message} |{exception.GetType().FullName}";
            // if (args.HasData())
            // {
            //     message += " {exception}";
            // }
        }

        //args = Merge(exception, otherArgs, args);
        var customProperties = ServiceProviderWrapper.GetScopedProperties()?.Properties;
        if (customProperties is null)
        {
            logger.Log(logLevel, exception, message, args!);
            return;
        }
        using (logger.BeginScope(customProperties))
        {
            logger.Log(logLevel, exception, message, args!);
        }

        // using (logger.BeginScope(customProperties))
        // {
        //     logger.Log(logLevel, exception, message, args!);
        // }

        // if (args.HasData())
        // {
        //     if (exception is not null)
        //     {
        //         logger.Log(logLevel, exception, message, args!, exception.Adapt<ExceptionInfo>());
        //     }
        //     else
        //     {
        //         logger.Log(logLevel, exception, message, args!);
        //     }
        // }
        // else
        // {
        //     if (exception is not null)
        //     {
        //         logger.Log(logLevel, exception, message, exception.Adapt<ExceptionInfo>());
        //     }
        //     else
        //     {
        //         logger.Log(logLevel, exception, message, args!);
        //     }
        // }
    }

    private static object?[]? Merge(Exception? exception, object?[]? otherArgs, params object?[]? args)
    {
        var length = exception is null ? 0 : 1;
        var combinedLength = (otherArgs?.Length ?? 0) + (args?.Length ?? 0) + length;
        if (combinedLength is 0)
        {
            return null;
        }

        var newArgs = new object[combinedLength];
        var secondCopyIndex = 0;
        if (args.HasData())
        {
            Array.Copy(args!, newArgs, args!.Length);
            secondCopyIndex = args.Length;
        }

        if (otherArgs.HasData())
        {
            Array.Copy(otherArgs!, 0, newArgs, secondCopyIndex, otherArgs!.Length);
        }

        if (exception is not null)
        {
            newArgs[^1] = exception;
        }

        return newArgs;
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
            LogLevel.None => NLogLevel.Off,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
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
using System;
using Hangfire.Logging;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Logging.Hangfire;

public class HangfireLogger : ILog
{
    public bool Log(LogLevel logLevel, Func<string>? messageFunc, Exception? exception = null)
    {
        if (messageFunc == null) return true;

        var message = messageFunc.Invoke();

        if (message.Contains("Hangfire.SqlServer.CountersAggregator") ||
            message.ToLower().Contains("countersaggregator"))
            return true;

        if (exception != null)
        {
            this.LogError(exception, message);
            return true;
        }

        switch (logLevel)
        {
            case LogLevel.Debug:
                this.LogDebug(message);
                break;
            case LogLevel.Error:
                this.LogError(message);
                break;
            case LogLevel.Info:
                this.LogInformation(message);
                break;
            case LogLevel.Fatal:
                this.LogCritical(message);
                break;
            case LogLevel.Trace:
                this.LogTrace(message);
                break;
            case LogLevel.Warn:
                this.LogWarning(message);
                break;
            default:
                this.LogInformation($"{logLevel} => {message}");
                break;
        }

        return true;
    }
}
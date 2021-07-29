using System;
using Hangfire.Logging;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Logging.Hangfire
{
    public class HangfireLogger : ILog
    {
        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception = null)
        {
            if (messageFunc == null) return true;

            var message = messageFunc.Invoke();

            if (message.Contains("Hangfire.SqlServer.CountersAggregator") ||
                message.ToLower().Contains("countersaggregator"))
                return true;

            if (exception != null)
            {
                this.ELogError(exception, message);
                return true;
            }

            switch (logLevel)
            {
                case LogLevel.Debug:
                    this.ELogDebug(message);
                    break;
                case LogLevel.Error:
                    this.ELogError(message);
                    break;
                case LogLevel.Info:
                    this.ELogInfo(message);
                    break;
                case LogLevel.Fatal:
                    this.ELogFatal(message);
                    break;
                case LogLevel.Trace:
                    this.ELogTrace(message);
                    break;
                case LogLevel.Warn:
                    this.ELogWarn(message);
                    break;
                default:
                    this.ELogInfo($"{logLevel} => {message}");
                    break;
            }

            return true;
        }
    }
}
using System;
using System.Globalization;
using NLog;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging.Nlog.Renderers;

namespace Solhigson.Framework.Logging;

public class LogWrapper
{
    private readonly Logger _logger;

    internal LogWrapper(string name)
    {
        _logger = NLog.LogManager.GetLogger(name);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }

    public bool IsDebugEnabled => _logger.IsDebugEnabled;

    internal void Log(string message, LogLevel logLevel, object data = null,
        Exception exception = null, string serviceName = null, string serviceType = null,
        string group = Constants.Group.AppLog, string status = null, string endPointUrl = null)
    {
        if (!_logger.IsEnabled(logLevel))
        {
            return;
        }
        var eventInfo = LogEventInfo.Create(logLevel, _logger.Name, exception, CultureInfo.InvariantCulture, message);
        //eventInfo.Exception = exception;
        eventInfo.TimeStamp = DateTime.UtcNow;
        eventInfo.Properties[CustomDataRenderer.Name] = data;
        eventInfo.Properties["serviceName"] = serviceName;
        eventInfo.Properties["serviceType"] = serviceType;
        eventInfo.Properties[GroupRenderer.Name] = group;
        eventInfo.Properties["status"] = status;
        eventInfo.Properties["url"] = endPointUrl;
        eventInfo.Properties[UserRenderer.Name] = ServiceProviderWrapper.HttpContextAccessor?.GetEmailClaim() ?? ServiceProviderWrapper.GetCurrentLogUserEmail();
        eventInfo.Properties["chainId"] = ServiceProviderWrapper.GetCurrentLogChainId();
        _logger.Log(eventInfo);
    }

    public void Debug(string message, object data = null)
    {
        if (_logger.IsDebugEnabled)
        {
            Log(message, LogLevel.Debug, data);
        }
    }

    public void Info(string message, object data = null)
    {
        if (_logger.IsInfoEnabled)
        {
            Log(message, LogLevel.Info, data);
        }
    }

    public void Warn(string message, object data = null)
    {
        if (_logger.IsWarnEnabled)
        {
            Log(message, LogLevel.Warn, data);
        }
    }

    public void Error(Exception e, string message = null, object data = null)
    {
        if (_logger.IsErrorEnabled)
        {
            Log(message, LogLevel.Error, data, e);
        }
    }

    public void Fatal(string message, Exception e = null, object data = null)
    {
        if (_logger.IsFatalEnabled)
        {
            Log(message, LogLevel.Fatal, data, e);
        }
    }

    public void Trace(string message, object data = null)
    {
        if (_logger.IsTraceEnabled)
        {
            Log(message, LogLevel.Trace, data);
        }
    }
}
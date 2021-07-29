﻿using System;
using NLog;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging.Nlog.Renderers;

namespace Solhigson.Framework.Logging
{
    public class LogWrapper
    {
        private readonly Logger _logger;

        internal LogWrapper(string name)
        {
            _logger = NLog.LogManager.GetLogger(name);
        }

        public bool IsDebugEnabled => _logger.IsDebugEnabled;

        internal void Log(string message, LogLevel logLevel, object data = null,
            Exception exception = null, string serviceName = null, string serviceType = null,
            string group = Constants.Group.AppLog, string status = null, string endPointUrl = null,
            string userEmail = null)
        {
            var eventInfo = LogEventInfo.Create(logLevel, _logger.Name, message);
            eventInfo.Exception = exception;
            eventInfo.TimeStamp = DateTime.UtcNow;
            eventInfo.Properties[CustomDataRenderer.Name] = data;
            eventInfo.Properties["serviceName"] = serviceName;
            eventInfo.Properties["serviceType"] = serviceType;
            eventInfo.Properties[GroupRenderer.Name] = group;
            eventInfo.Properties["status"] = status;
            eventInfo.Properties["url"] = endPointUrl;
            var authenticatedEmail = LogManager.HttpContextAccessor.GetUserEmail();
            if (string.IsNullOrWhiteSpace(authenticatedEmail))
            {
                authenticatedEmail = userEmail;
            }

            eventInfo.Properties[UserRenderer.Name] = authenticatedEmail;
            _logger.Log(typeof(LogWrapper), eventInfo);
        }

        public void Debug(string message, object data = null, string userEmail = null)
        {
            Log(message, LogLevel.Debug, data, userEmail: userEmail);
        }

        public void Info(string message, object data = null, string userEmail = null)
        {
            Log(message, LogLevel.Info, data, userEmail: userEmail);
        }

        public void Warn(string message, object data = null, string userEmail = null)
        {
            Log(message, LogLevel.Warn, data, userEmail: userEmail);
        }

        public void Error(Exception e, string message = null, object data = null, string userEmail = null)
        {
            Log(message, LogLevel.Error, data, e, userEmail: userEmail);
        }

        public void Fatal(string message, Exception e = null, object data = null, string userEmail = null)
        {
            Log(message, LogLevel.Fatal, data, e, userEmail: userEmail);
        }

        public void Trace(string message, object data = null, string userEmail = null)
        {
            Log(message, LogLevel.Trace, data, userEmail: userEmail);
        }
    }
}
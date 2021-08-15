using System;
using NLog;
using NLog.Common;
using NLog.Targets;
using Solhigson.Framework.Services;

namespace Solhigson.Framework.Logging.Nlog.Targets
{
    [Target("AzureLogAnalytics")]
    public class AzureLogAnalyticsTarget : TargetWithLayout
    {
        private static AzureLogAnalyticsService _analyticsService;

        public AzureLogAnalyticsTarget(string workspaceId, string sharedKey, string logName)
        {
            _analyticsService = new AzureLogAnalyticsService(workspaceId, sharedKey, logName);
        }

        protected override void InitializeTarget()
        {
        }

        protected override void Write(LogEventInfo logEvent)
        {
            try
            {
                var log = Layout.Render(logEvent);
                if (_analyticsService.PostLog(log))
                {
                    return;
                }

                InternalLogger.Log(logEvent.Level, log);
            }
            catch (Exception ex)
            {
                InternalLogger.Error(ex, "Error while sending log messages to Azure Log Analytics");
            }
        }
    }
}
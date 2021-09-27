using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NLog.Common;
using Solhigson.Framework.AzureLogAnalytics.Nlog;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Logging.Nlog;
using Solhigson.Framework.Logging.Nlog.Dto;

namespace Solhigson.Framework.AzureLogAnalytics
{
    public static class AzureLogAnalyticsExtensions
    {
        public static IApplicationBuilder UseSolhigsonNLogAzureLogAnalyticsTarget(this IApplicationBuilder app,
            DefaultNLogAzureLogAnalyticsParameters defaultNLogAzureLogAnalyticsParameters = null)
        {
            if (string.IsNullOrWhiteSpace(defaultNLogAzureLogAnalyticsParameters?.AzureAnalyticsWorkspaceId)
                || string.IsNullOrWhiteSpace(defaultNLogAzureLogAnalyticsParameters?.AzureAnalyticsSharedSecret)
                || string.IsNullOrWhiteSpace(defaultNLogAzureLogAnalyticsParameters?.AzureAnalyticsLogName))
            {
                app.UseSolhigsonNLogDefaultFileTarget();
                InternalLogger.Error(
                    "Unable to initalize NLog Azure Analytics Target because one or more the the required parameters are missing: " +
                    "[WorkspaceId, Sharedkey or LogName].");
                return app;
            }

            app.ConfigureSolhigsonNLogDefaults();
            var customTarget = new AzureLogAnalyticsTarget(defaultNLogAzureLogAnalyticsParameters.AzureAnalyticsWorkspaceId, 
                defaultNLogAzureLogAnalyticsParameters.AzureAnalyticsSharedSecret, defaultNLogAzureLogAnalyticsParameters.AzureAnalyticsLogName,
                app.ApplicationServices.GetRequiredService<IHttpClientFactory>())
            {
                Name = "custom document",
                Layout = NLogDefaults.GetDefaultJsonLayout(),
            };

            app.UseSolhigsonNLogCustomTarget(new CustomNLogTargetParameters(customTarget));
            return app;
        }
        
    }
}
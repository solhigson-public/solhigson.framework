using Microsoft.AspNetCore.Builder;
using NLog.Common;
using Solhigson.Framework.AzureCosmosDb.Dto;
using Solhigson.Framework.AzureCosmosDb.Logging.Nlog;
using Solhigson.Framework.Logging.Dto;
using Solhigson.Framework.Logging.Nlog;

namespace Solhigson.Framework.Extensions
{
    public static class Extensions
    {
        public static IApplicationBuilder UseSolhigsonNLogAzureLogAnalyticsTarget(this IApplicationBuilder app,
            NLogCosmosDbParameters parameters = null)
        {
            if (string.IsNullOrWhiteSpace(parameters?.Container)
                || string.IsNullOrWhiteSpace(parameters?.ConnectionString)
                || string.IsNullOrWhiteSpace(parameters?.Database))
            {
                app.UseSolhigsonNLogDefaultFileTarget();
                InternalLogger.Error(
                    "Unable to initalize NLog Azure Cosmos Db Target because one or more the the required parameters are missing: " +
                    "[ConnectionString, Database or Container].");
                return app;
            }

            var customTarget = new CosmosDbTarget<CosmosDbLog>(parameters.ConnectionString, 
                parameters.Database, parameters.Container)
            {
                Name = "custom document",
                Layout = NLogDefaults.GetDefaultJsonLayout(),
            };

            app.UseSolhigsonNLogCustomTarget(new CustomNLogTargetParameters(customTarget));
            return app;
        }

    }
}
using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Cosmos;
using NLog.Common;
using Solhigson.Framework.AzureCosmosDb;
using Solhigson.Framework.AzureCosmosDb.Dto;
using Solhigson.Framework.AzureCosmosDb.Logging.Nlog;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Logging.Dto;
using Solhigson.Framework.Logging.Nlog;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Extensions
{
    public static class Extensions
    {
        private static readonly LogWrapper Logger = LogManager.GetCurrentClassLogger();
        public static CosmosDbService UseSolhigsonNLogAzureLogAnalyticsTarget(this IApplicationBuilder app,
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
                return null;
            }
            
            var service = CreateLogContainer(parameters);
            if (service == null)
            {
                Logger.Warn($"CreateLogContainer for container: {parameters.Container} returned null, not initializing NLogCosmosDb Target");
                return null;
            }
           
            var customTarget = new CosmosDbTarget<CosmosDbLog>(parameters.ConnectionString, 
                parameters.Database, parameters.Container)
            {
                Name = "custom document",
                Layout = NLogDefaults.GetDefaultJsonLayout(),
            };

            app.UseSolhigsonNLogCustomTarget(new CustomNLogTargetParameters(customTarget));
            return service;
        }
        
        private static CosmosDbService CreateLogContainer(NLogCosmosDbParameters parameters)
        {
            try
            {
                var client = new CosmosClient(parameters.ConnectionString);
                var tp = ThroughputProperties.CreateManualThroughput(400);
                var database = AsyncTools.RunSync(() => client.CreateDatabaseIfNotExistsAsync(parameters.Database, tp));
                if (database == null)
                {
                    Logger.Warn($"Unable to create CosmosDbService for container: {parameters.Container} as Database initialize failed");
                    return null;
                }
                var containerResponse = database.Database
                    .CreateContainerIfNotExistsAsync(parameters.Container, "/id").Result;

                Logger.Info($"{parameters.Container} on database {database.Database.Id} create status: {containerResponse.StatusCode}");
            
                if (containerResponse.StatusCode == HttpStatusCode.Created)
                {
                    Logger.Info($"{parameters.Container} created, updating indexes");

                    containerResponse.Resource.DefaultTimeToLive = (int)TimeSpan.FromDays(7).TotalSeconds;

                    AsyncTools.RunSync(() => containerResponse.Container.ReplaceContainerAsync(containerResponse.Resource));
                }
            
                return new CosmosDbService(database.Database.Client, database.Database.Id, parameters.Container);
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Creating CosmosDbService for Container: {parameters.Container}");
            }

            return null;
        }


    }
}
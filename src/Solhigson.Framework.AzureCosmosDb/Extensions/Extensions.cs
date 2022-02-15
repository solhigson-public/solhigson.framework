using System;
using System.Net;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Cosmos;
using NLog.Common;
using Solhigson.Framework.AzureCosmosDb;
using Solhigson.Framework.AzureCosmosDb.Dto;
using Solhigson.Framework.AzureCosmosDb.Logging.Nlog;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Logging.Nlog;
using Solhigson.Framework.Logging.Nlog.Dto;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Extensions
{
    public static class Extensions
    {
        private static readonly LogWrapper Logger = LogManager.GetLogger(typeof(Extensions).FullName);
        public static CosmosDbService UseSolhigsonNLogCosmosDbTarget(this IApplicationBuilder app,
            NLogCosmosDbParameters parameters = null)
        {
            if (string.IsNullOrWhiteSpace(parameters?.Container)
                || parameters?.Database == null)
            {
                app.UseSolhigsonNLogDefaultFileTarget();
                InternalLogger.Error(
                    "Unable to initalize NLog Azure Cosmos Db Target because one or more the the required parameters are missing: " +
                    "[Database or Container].");
                return null;
            }
            app.ConfigureSolhigsonNLogDefaults(parameters);
           
            var ttl = parameters.ExpireAfter ?? TimeSpan.FromDays(1);
            var service = CreateLogContainer(parameters, ttl);
            if (service == null)
            {
                Logger.Warn($"CreateLogContainer for container: {parameters.Container} returned null, not initializing NLogCosmosDb Target");
                return null;
            }
           
            var customTarget = new CosmosDbTarget<CosmosDbLog>(parameters.Database, parameters.Container, ttl)
            {
                Name = "custom document",
                Layout = NLogDefaults.GetDefaultJsonLayout2(),
            };

            var customTargetParameters = new CustomNLogTargetParameters(customTarget);
            parameters.Adapt(customTargetParameters);
            app.UseSolhigsonNLogCustomTarget(customTargetParameters);
            return service;
        }

        private static CosmosDbService CreateLogContainer(NLogCosmosDbParameters parameters, TimeSpan ttl)
        {
            try
            {
                var containerResponse = parameters.Database
                    .CreateContainerIfNotExistsAsync(parameters.Container, "/id").Result;

                Logger.Info(
                    $"{parameters.Container} on database {parameters.Database.Id} create status: {containerResponse.StatusCode}");

                if (containerResponse.StatusCode == HttpStatusCode.Created)
                {
                    Logger.Info($"{parameters.Container} created, updating indexes");
                    containerResponse.Resource.IndexingPolicy.IndexingMode = IndexingMode.Consistent;
                    containerResponse.Resource.IndexingPolicy.IncludedPaths.Clear();

                    containerResponse.Resource.IndexingPolicy.IncludedPaths.Add(
                        new IncludedPath { Path = "/ChainId/?" });
                    containerResponse.Resource.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/Group/?" });
                    containerResponse.Resource.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/_ts/?" });
                    containerResponse.Resource.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/Data/?" });
                    containerResponse.Resource.IndexingPolicy.IncludedPaths.Add(new IncludedPath
                        { Path = "/ServiceUrl/?" });
                    containerResponse.Resource.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/User/?" });
                    containerResponse.Resource.IndexingPolicy.IncludedPaths.Add(new IncludedPath
                        { Path = "/Exception/?" });
                    containerResponse.Resource.IndexingPolicy.IncludedPaths.Add(new IncludedPath
                        { Path = "/Source/?" });
                    containerResponse.Resource.IndexingPolicy.IncludedPaths.Add(new IncludedPath
                        { Path = "/Description/?" });

                    containerResponse.Resource.IndexingPolicy.ExcludedPaths.Clear();
                    containerResponse.Resource.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/*" });

                    containerResponse.Resource.DefaultTimeToLive = (int)ttl.TotalSeconds;

                    AsyncTools.RunSync(() =>
                        containerResponse.Container.ReplaceContainerAsync(containerResponse.Resource));
                }

                return new CosmosDbService(parameters.Database.Client, parameters.Database.Id, parameters.Container);
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Creating CosmosDbService for Container: {parameters.Container}");
            }

            return null;
        }


    }
}
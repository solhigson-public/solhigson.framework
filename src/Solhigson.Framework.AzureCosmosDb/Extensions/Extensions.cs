using System;
using System.Net;
using Mapster;
using MassTransit;
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

namespace Solhigson.Framework.Extensions;

public static class Extensions
{
    private static readonly LogWrapper Logger = LogManager.GetLogger(typeof(Extensions).FullName);
    public static CosmosDbInitializationResult UseSolhigsonNLogCosmosDbTarget(this IApplicationBuilder app,
        NLogCosmosDbParameters parameters = null)
    {
        var result = new CosmosDbInitializationResult();
        if (string.IsNullOrWhiteSpace(parameters?.Container)
            || parameters?.Database == null)
        {
            app.UseSolhigsonNLogDefaultFileTarget();
            InternalLogger.Error(
                "Unable to initalize NLog Azure Cosmos Db Target because one or more the the required parameters are missing: " +
                "[Database or Container].");
            return result;
        }
        app.ConfigureSolhigsonNLogDefaults(parameters);
           
        var ttl = parameters.ExpireAfter ?? TimeSpan.FromDays(1);
        result.LogContainerInitializationSuccess = CreateLogContainer(parameters, ttl);
        if (!result.LogContainerInitializationSuccess)
        {
            Logger.Warn($"CreateLogContainer for container: {parameters.Container} returned null, not initializing NLogCosmosDb Target");
            return result;
        }
        result.AuditContainerInitializationSuccess = CreateAuditContainer(parameters);
        var customTarget = new CosmosDbTarget<CosmosDbLog>(parameters.Database, parameters.Container, ttl)
        {
            Name = "custom document",
            Layout = NLogDefaults.GetDefaultJsonLayout2(),
        };

        var customTargetParameters = new CustomNLogTargetParameters(customTarget);
        parameters.Adapt(customTargetParameters);
        app.UseSolhigsonNLogCustomTarget(customTargetParameters);
        return result;
    }

    private static bool CreateLogContainer(NLogCosmosDbParameters parameters, TimeSpan ttl)
    {
        try
        {
            var containerResponse = parameters.Database
                .CreateContainerIfNotExistsAsync(parameters.Container, "/id").Result;

            Logger.Debug(
                $"{parameters.Container} on database {parameters.Database.Id} create status: {containerResponse.StatusCode}");

            if (containerResponse.StatusCode == HttpStatusCode.Created)
            {
                Logger.Debug($"{parameters.Container} created, updating indexes");
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

            return true;
        }
        catch (Exception e)
        {
            Logger.Error(e, $"Creating CosmosDbService for Container: {parameters.Container}");
        }

        return false;
    }

    private static bool CreateAuditContainer(NLogCosmosDbParameters parameters)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(parameters.AuditContainer))
            {
                return false;
            }
            var containerResponse = parameters.Database
                .CreateContainerIfNotExistsAsync(parameters.AuditContainer, "/id").Result;

            if (containerResponse.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK)
            {
                Audit.Core.Configuration.DataProvider = new Audit.AzureCosmos.Providers.AzureCosmosDataProvider(
                    config => config
                        .Database(parameters.Database.Id)
                        .Container(parameters.AuditContainer)
                        .CosmosClient(parameters.Database.Client)
                        .WithId(_ => NewId.NextSequentialGuid().ToString()));
            }

            if (!parameters.AuditLogExpireAfter.HasValue || containerResponse.StatusCode != HttpStatusCode.Created)
            {
                return true;
            }
            Logger.Debug($"{parameters.Container} created, setting ttl");
            containerResponse.Resource.DefaultTimeToLive = (int)parameters.AuditLogExpireAfter.Value.TotalSeconds;

            AsyncTools.RunSync(() =>
                containerResponse.Container.ReplaceContainerAsync(containerResponse.Resource));
            return true;
        }
        catch (Exception e)
        {
            Logger.Error(e, $"Creating CosmosDbService for Container: {parameters.AuditContainer}");
        }

        return false;
    }


}
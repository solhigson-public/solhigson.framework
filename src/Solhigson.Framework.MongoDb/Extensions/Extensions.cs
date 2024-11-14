using Audit.Core;
using Audit.MongoDB.Providers;
using Microsoft.AspNetCore.Builder;
using MongoDB.Driver;
using NLog.Common;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Logging;
using Solhigson.Framework.MongoDb.Dto;
using Solhigson.Framework.MongoDb.Services;

namespace Solhigson.Framework.MongoDb.Extensions;

public static class Extensions
{
    private static readonly LogWrapper Logger = LogManager.GetLogger(typeof(Extensions).FullName);
    public static MongoDbService<MongoDbLog>? ConfigureAuditingWithMongoDb(this IApplicationBuilder app,
        AuditParameters? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(parameters?.AuditCollection) || string.IsNullOrWhiteSpace(parameters?.Database)
                                                                   || string.IsNullOrWhiteSpace(parameters?.ConnectionString))
        {
            Logger.LogError(
                "Unable to initialize Mongo Db for Auditing because one or more of the required parameters are missing: {parameters}" +
                "[ConnectionString, Database or AuditCollection].", parameters!);
            return null;
        }

        var service = MongoDbServiceFactory.Create<MongoDbLog>(parameters.ConnectionString, parameters.Database, parameters.AuditCollection);
        if (service == null)
        {
            Logger.LogError("Unable to create Mongo Db service with supplied parameters: {parameters}, check log for error.", parameters);
            return null;
        }

        var dataProvider = new MongoDataProvider
        {
            ConnectionString = parameters.ConnectionString,
            Database = parameters.Database,
            Collection = parameters.AuditCollection
        };
        
        Configuration.DataProvider = dataProvider;

        if (!parameters.AuditLogExpireAfter.HasValue)
        {
            return null;
        }
        
        var ttlIndex = Builders<AuditEvent>.IndexKeys.Descending(t => t.StartDate);
        dataProvider.GetMongoCollection().Indexes.CreateOne(
            new CreateIndexModel<AuditEvent>(ttlIndex, new CreateIndexOptions
                {
                    ExpireAfter = parameters.AuditLogExpireAfter,
                    Name = "LogsExpireIndex",
                    Background = true
                }
            ));


        return service;
    }

    private static void ConfigureAuditCollection(AuditParameters loggingParameters)
    {
    }
        
}
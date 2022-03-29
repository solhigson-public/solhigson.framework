using System;
using Audit.Core;
using Audit.MongoDB.Providers;
using Mapster;
using Microsoft.AspNetCore.Builder;
using MongoDB.Driver;
using NLog.Common;
using NLog.Layouts;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Logging.Nlog;
using Solhigson.Framework.Logging.Nlog.Dto;
using Solhigson.Framework.MongoDb.Dto;
using Solhigson.Framework.MongoDb.Logging.NLog;
using Solhigson.Framework.MongoDb.Services;

namespace Solhigson.Framework.Extensions;

public static class Extensions
{
    private static readonly LogWrapper Logger = LogManager.GetLogger(typeof(Extensions).FullName);
    public static MongoDbService<MongoDbLog> UseSolhigsonNLogMongoDbTarget(this IApplicationBuilder app,
        NlogMongoDbParameters parameters = null)
    {
        if (string.IsNullOrWhiteSpace(parameters?.Collection)
            || string.IsNullOrWhiteSpace(parameters?.Database) || string.IsNullOrWhiteSpace(parameters?.ConnectionString))
        {
            app.UseSolhigsonNLogDefaultFileTarget();
            InternalLogger.Error(
                "Unable to initalize NLog Mongo Db Db Target because one or more the the required parameters are missing: " +
                "[ConnectionString, Database or Collection].");
            return null;
        }
        app.ConfigureSolhigsonNLogDefaults(parameters);
        var (service, expireAfter) = CreateDefaultCollection(parameters);
        if (service is null)
        {
            return null;
        }
        ConfigureAuditCollection(parameters);
           

        var layout = NLogDefaults.GetDefaultJsonLayout2(parameters.EncodeChildJsonContent);
        layout.Attributes.Add(new JsonAttribute("Id", "${guid}", true));
        layout.Attributes.Add(new JsonAttribute("Timestamp", "${solhigson-timestamp}", true));
        var customTarget = new MongoDbTarget<MongoDbLog>(service, expireAfter)
        {
            Name = "custom document",
            Layout = layout,
        };

        var customTargetParameters = new CustomNLogTargetParameters(customTarget);
        parameters.Adapt(customTargetParameters);
        app.UseSolhigsonNLogCustomTarget(customTargetParameters);
        return service;
    }

    private static (MongoDbService<MongoDbLog>, TimeSpan) CreateDefaultCollection(NlogMongoDbParameters parameters)
    {
        var service = MongoDbServiceFactory.Create<MongoDbLog>(parameters.ConnectionString, parameters.Database, parameters.Collection);
        if (service == null)
        {
            InternalLogger.Error($"Unable to create Mongo Db service with supplied parameters, check log for errror.");
            return (null, TimeSpan.Zero);
        }

        var expireAfter = parameters.ExpireAfter ?? TimeSpan.FromDays(1);
        var ttlIndex = Builders<MongoDbLog>.IndexKeys.Descending(t => t.Ttl);
        var chainIndex = Builders<MongoDbLog>.IndexKeys.Descending(t => t.ChainId);
        var composite = Builders<MongoDbLog>.IndexKeys.Descending(t => t.Timestamp)
            .Descending(t => t.Data)
            .Descending(t => t.User)
            .Descending(t => t.ServiceUrl)
            .Descending(t => t.Exception)
            .Descending(t => t.Source)
            .Descending(t => t.Group)
            .Descending(t => t.Description);

        var generalCreateIndexOptions = new CreateIndexOptions
        {
            Sparse = true,
            Background = true
        };
        service.Collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<MongoDbLog>(ttlIndex, new CreateIndexOptions { 
                ExpireAfter = TimeSpan.Zero,
                Name = "LogsExpireIndex", 
                Background = true 
            }),
            new CreateIndexModel<MongoDbLog>(chainIndex, generalCreateIndexOptions),
            new CreateIndexModel<MongoDbLog>(composite, generalCreateIndexOptions),
        });
        return (service, expireAfter);
    }

    private static void ConfigureAuditCollection(NlogMongoDbParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.AuditCollection))
        {
            return;
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
            return;
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
    }
        
}
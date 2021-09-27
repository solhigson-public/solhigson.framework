using Microsoft.AspNetCore.Builder;
using NLog.Common;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Logging.Nlog;
using Solhigson.Framework.Logging.Nlog.Dto;
using Solhigson.Framework.MongoDb.Dto;
using Solhigson.Framework.MongoDb.Nlog;

namespace Solhigson.Framework.MongoDb
{
    public static class MongoDbExtensions
    {
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
            
            var service = MongoDbServiceFactory.Create<MongoDbLog>(parameters.ConnectionString, parameters.Database, parameters.Collection);
            if (service == null)
            {
                InternalLogger.Error($"Unable to create Mongo Db service with supplied parameters, check log for errror.");
                return null;
            }

            app.ConfigureSolhigsonNLogDefaults();
            var customTarget = new MongoDbTarget<MongoDbLog>(service)
            {
                Name = "custom document",
                Layout = NLogDefaults.GetDefaultNoSqlDbJsonLayout(),
            };

            app.UseSolhigsonNLogCustomTarget(new CustomNLogTargetParameters(customTarget));
            return service;
        }

    }
}
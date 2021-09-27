using Microsoft.AspNetCore.Builder;
using NLog.Common;
using NLog.Layouts;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Logging.Dto;
using Solhigson.Framework.Logging.Nlog;
using Solhigson.Framework.MongoDb.Dto;
using Solhigson.Framework.MongoDb.Logging.NLog;
using Solhigson.Framework.MongoDb.Services;

namespace Solhigson.Framework.Extensions
{
    public static class Extensions
    {
        private static readonly LogWrapper Logger = LogManager.GetCurrentClassLogger();
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

            var layout = NLogDefaults.GetDefaultJsonLayout(parameters.EncodeChildJsonContent);
            layout.Attributes.Add(new JsonAttribute("Id", "${guid}", true));
            layout.Attributes.Add(new JsonAttribute("Timestamp", "${solhigson-timestamp}", true));
            
            //app.ConfigureSolhigsonNLogDefaults();
            var customTarget = new MongoDbTarget<MongoDbLog>(service)
            {
                Name = "custom document",
                Layout = layout,
            };

            app.UseSolhigsonNLogCustomTarget(new CustomNLogTargetParameters(customTarget));
            return service;
        }
        
    }
}
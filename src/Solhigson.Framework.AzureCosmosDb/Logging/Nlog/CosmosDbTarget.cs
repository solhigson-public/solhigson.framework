using System;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using NLog;
using NLog.Common;
using NLog.Targets;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.AzureCosmosDb.Logging.Nlog
{
    public class CosmosDbTarget<T> : TargetWithLayout
    {
        private CosmosDbService _service;

        public CosmosDbTarget(string connectionString, string database, string container)
        {
            _service = new CosmosDbService(new CosmosClient(connectionString, new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway
            }), database, container);
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var log = Layout.Render(logEvent);
            if (SendToAzureCosmosDb(log))
            {
                return;
            }
            InternalLogger.Log(logEvent.Level, log);
        }


        private bool SendToAzureCosmosDb(string jsonString)
        {
            try
            {
                var document = JsonConvert.DeserializeObject<T>(jsonString);
                AsyncTools.RunSync(() => _service.AddDocumentAsync(document));
                return true;
            }
            catch (Exception e)
            {
                InternalLogger.Error(e, "Error while sending log messages to Azure Cosmos Db");
                return false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            _service = null;
            base.Dispose(disposing);
        }
    }
}
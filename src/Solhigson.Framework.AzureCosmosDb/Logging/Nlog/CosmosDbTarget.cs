using System;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using NLog.Targets;

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
        
        public void CreateJson(string jsonString)
        {            
            var document = JsonConvert.DeserializeObject<T>(jsonString);
            //AsyncTools.RunSync(() => _service.AddDocumentAsync(document));
        }

        protected override void Dispose(bool disposing)
        {
            _service = null;
            base.Dispose(disposing);
        }
    }
}
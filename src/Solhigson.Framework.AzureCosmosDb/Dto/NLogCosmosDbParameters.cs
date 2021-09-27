using Microsoft.Azure.Cosmos;
using Solhigson.Framework.Logging.Nlog.Dto;

namespace Solhigson.Framework.AzureCosmosDb.Dto
{
    public class NLogCosmosDbParameters : DefaultNLogParameters
    {
        public Database Database { get; set; }
        public string Container { get; set; }
    }
}
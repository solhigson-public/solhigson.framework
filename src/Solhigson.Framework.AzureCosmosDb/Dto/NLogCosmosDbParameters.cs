using Solhigson.Framework.Logging.Dto;

namespace Solhigson.Framework.AzureCosmosDb.Dto
{
    public class NLogCosmosDbParameters : DefaultNLogParameters
    {
        public string ConnectionString { get; set; }
        public string Database { get; set; }
        public string Container { get; set; }
    }
}
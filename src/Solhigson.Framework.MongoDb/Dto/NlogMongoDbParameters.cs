using Solhigson.Framework.Logging.Nlog.Dto;

namespace Solhigson.Framework.MongoDb.Dto
{
    public class NlogMongoDbParameters : DefaultNLogParameters
    {
        public string ConnectionString { get; set; }
        public string Database { get; set; }
        public string Collection { get; set; }
    }
}
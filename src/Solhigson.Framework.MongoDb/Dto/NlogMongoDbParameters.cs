using System;
using Solhigson.Framework.Logging.Nlog.Dto;

namespace Solhigson.Framework.MongoDb.Dto;

public class NlogMongoDbParameters : DefaultNLogParameters
{
    public NlogMongoDbParameters()
    {
        EncodeChildJsonContent = true;
    }
    public string ConnectionString { get; set; }
    public string Database { get; set; }
    public string Collection { get; set; }
        
    public TimeSpan? ExpireAfter { get; set; }

}
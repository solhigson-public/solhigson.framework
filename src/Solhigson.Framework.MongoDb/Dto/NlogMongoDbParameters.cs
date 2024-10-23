using System;
using Solhigson.Framework.Logging.Nlog.Dto;

namespace Solhigson.Framework.MongoDb.Dto;

public class NlogMongoDbParameters
{
    public NlogMongoDbParameters()
    {
        EncodeChildJsonContent = true;
    }

    public bool EncodeChildJsonContent { get; set; }

    public string? ConnectionString { get; set; }
    public string? Database { get; set; }
    public string? AuditCollection { get; set; }
        
    public TimeSpan? AuditLogExpireAfter { get; set; }

}
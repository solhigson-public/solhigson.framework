using System;
using Solhigson.Framework.Logging.Nlog.Dto;

namespace Solhigson.Framework.MongoDb.Dto;

public class AuditParameters
{
    public bool EncodeChildJsonContent { get; set; } = true;

    public string? ConnectionString { get; set; }
    public string? Database { get; set; }
    public TimeSpan? AuditLogExpireAfter { get; set; }

}
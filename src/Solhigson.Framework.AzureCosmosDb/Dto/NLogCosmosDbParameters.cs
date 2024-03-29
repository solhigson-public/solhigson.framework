﻿using System;
using Microsoft.Azure.Cosmos;
using Solhigson.Framework.Logging.Nlog.Dto;

namespace Solhigson.Framework.AzureCosmosDb.Dto;

public class NLogCosmosDbParameters : DefaultNLogParameters
{
    public Database Database { get; set; }
    public string Container { get; set; }
    public string AuditContainer { get; set; }
        
    public TimeSpan? ExpireAfter { get; set; }
    public TimeSpan? AuditLogExpireAfter { get; set; }

}
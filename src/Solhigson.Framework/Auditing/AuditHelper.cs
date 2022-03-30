using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;
using Solhigson.Framework.Logging;

namespace Solhigson.Framework.Auditing;

public static class AuditHelper
{
    private static readonly LogWrapper Logger = LogManager.GetLogger(typeof(AuditHelper).FullName);
    
    public static async Task AuditAsync(string eventType, Dictionary<string, object> info)
    {
        await AuditInternalAsync(eventType, new AuditInfo
        {
            AuditData = info
        });
    }
    
    public static async Task AuditAsync(string eventType)
    {
        await AuditInternalAsync(eventType, null);
    }

    public static async Task AuditAsync(string eventType, string key, string value)
    {
        await AuditInternalAsync(eventType, new AuditInfo
        {
            AuditData = new Dictionary<string, object>
            {
                {key, value}
            }
        });
    }

    private static async Task AuditInternalAsync(string eventType, AuditInfo auditInfo)
    {
        try
        {
            await AuditScope.LogAsync(eventType, auditInfo);
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }
    }
    
}
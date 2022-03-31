using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;
using Solhigson.Framework.Logging;

namespace Solhigson.Framework.Auditing;

public static class AuditHelper
{
    private static readonly LogWrapper Logger = LogManager.GetLogger(typeof(AuditHelper).FullName);

    public static async Task AuditAsync(string eventType, List<AuditEntry> entries)
    {
        await AuditInternalAsync(eventType, new AuditInfo
        {
            Entries = entries
        });
    }

    public static async Task AuditAsync(string eventType)
    {
        await AuditInternalAsync(eventType, null);
    }

    public static async Task AuditAsync(string eventType, string propertyName, string oldValue, string newValue)
    {
        await AuditInternalAsync(eventType, new AuditInfo
        {
            Entries = new List<AuditEntry>
            {
                new()
                {
                    Changes = new List<AuditChange>
                    {
                        new()
                        {
                            ColumnName = propertyName,
                            OriginalValue = oldValue,
                            NewValue = newValue
                        }
                    }
                }
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
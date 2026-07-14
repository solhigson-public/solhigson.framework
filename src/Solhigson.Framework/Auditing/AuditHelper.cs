using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solhigson.Framework.Auditing;

public static class AuditHelper
{
    [Obsolete("Audit.NET pipeline retired; migrate to IAuditTrailService.LogAsync. No-op until removed.")]
    public static Task AuditAsync(string eventType, List<AuditEntry> entries) => Task.CompletedTask;

    [Obsolete("Audit.NET pipeline retired; migrate to IAuditTrailService.LogAsync. No-op until removed.")]
    public static Task AuditAsync(string eventType) => Task.CompletedTask;

    [Obsolete("Audit.NET pipeline retired; migrate to IAuditTrailService.LogAsync. No-op until removed.")]
    public static Task AuditAsync(string eventType, string propertyName, string oldValue, string newValue) => Task.CompletedTask;
}

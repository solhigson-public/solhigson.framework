using System;
using System.Collections.Generic;

namespace Solhigson.Framework.Auditing;

[Obsolete("Audit.NET pipeline retired; migrate to IAuditTrailService.LogAsync. Retained for external consumers until removed.")]
public class AuditInfo
{
    public List<AuditEntry>? Entries { get; set; }
}

[Obsolete("Audit.NET pipeline retired; migrate to IAuditTrailService.LogAsync. Retained for external consumers until removed.")]
public class AuditEntry
{
    public string? Table { get; set; }
    public string? PrimaryKey { get; set; }
    public string? Action { get; set; }
    public List<AuditChange>? Changes { get; set; }
}

[Obsolete("Audit.NET pipeline retired; migrate to IAuditTrailService.LogAsync. Retained for external consumers until removed.")]
public class AuditChange
{
    public string? ColumnName { get; set; }
    public string? OriginalValue { get; set; }
    public string? NewValue { get; set; }
}

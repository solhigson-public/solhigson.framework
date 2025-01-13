using System.Collections.Generic;

namespace Solhigson.Framework.Auditing;

public class AuditInfo
{
    public List<AuditEntry>? Entries { get; set; }
}

public class AuditEntry
{
    public string? Table { get; set; }
    public string? PrimaryKey { get; set; }
    public string? Action { get; set; }
    public List<AuditChange>? Changes { get; set; }
}
public class AuditChange
{
    public string? ColumnName { get; set; }
    public string? OriginalValue { get; set; }
    public string? NewValue { get; set; }
}

namespace Solhigson.Framework.EfCore.Caching;

public class EntityChangeMonitor : ChangeMonitor
{
    private readonly TableChangeTracker _tableChangeTracker;
    public TableChangeMonitor(TableChangeTracker tableChangeTracker)
    {
        UniqueId = $"{tableChangeTracker.TableNames}_{Guid.NewGuid().ToString()}";
        this.LogTrace($"New Change monitor: {UniqueId}");
        _tableChangeTracker = tableChangeTracker;
        _tableChangeTracker.OnChanged += TableChangeTrackerOnChanged;
        InitializationComplete();
    }

    private void TableChangeTrackerOnChanged(object? sender, EventArgs e)
    {
        this.LogTrace($"Monitor changed for {UniqueId}");
        OnChanged(UniqueId);
    }

    protected override void Dispose(bool disposing)
    {
        this.LogTrace($"Dispose called for {UniqueId}");
        _tableChangeTracker.OnChanged -= TableChangeTrackerOnChanged;
    }

    public override string UniqueId { get; }
        
        
}
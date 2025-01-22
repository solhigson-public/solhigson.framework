using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.EfCore.Caching;

public class EntityChangeTrackerHandler : IDisposable
{
    public event EventHandler? OnChanged;
    private readonly MemoryCacheProvider _memoryCacheProvider;

    private readonly Dictionary<string, int> _changeIds = new();

    public EntityChangeTrackerHandler(MemoryCacheProvider memoryCacheProvider, Type[] types)
    {
        _memoryCacheProvider = memoryCacheProvider;
        foreach (var type in types)
        {
            var name = EfCoreCacheManager.GetTypeName(type);
            _changeIds.Add(name, _memoryCacheProvider.GetEntityChangeTrackerChangeId(name).Result);
        }

        _memoryCacheProvider.OnTableChangeTimerElapsed += OnTableChangeTimerElapsed;
    }
    internal string TableNames => MemoryCacheProvider.Flatten(_changeIds.Keys.ToList());

    private void OnTableChangeTimerElapsed(object? sender, EventArgs e)
    {
        if (e is not EntityChangeTrackerEventArgs ce)
        {
            return;
        }

        foreach (var key in _changeIds.Keys)
        {
            if (!ce.ChangeIds.TryGetValue(key, out var changeId) || _changeIds[key] == changeId)
            {
                continue;
            }

            _changeIds[key] = changeId;
            this.LogTrace($"Change tracker changed for [{key}]");
            OnChanged?.Invoke(null, EventArgs.Empty);
        }


        /*
        if (!ce.ChangeIds.TryGetValue(TableName, out var changeId))
        {
            return;
        }

        if (changeId == _currentChangeTrackId)
        {
            return;
        }

        */
        /*
        _currentChangeTrackId = changeId;
        this.ELogDebug($"Change tracker changed for [{TableName}]");
        OnChanged?.Invoke(null, new EventArgs());
    */
    }

    public void Dispose()
    {
        _memoryCacheProvider.OnTableChangeTimerElapsed -= OnTableChangeTimerElapsed;
        OnChanged = null;
    }
}
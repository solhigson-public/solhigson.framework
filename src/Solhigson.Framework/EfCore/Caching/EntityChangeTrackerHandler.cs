using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Primitives;
using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.EfCore.Caching;

public class EntityChangeTrackerHandler : IDisposable
{
    private readonly MemoryCacheProvider _memoryCacheProvider;
    private volatile CancellationTokenSource _sentinel = new();
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
    
    internal CancellationChangeToken Sentinel() => new(_sentinel.Token);

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
            this.LogTrace("Change tracker changed for [{Key}]", key);
            var old = _sentinel;
            _sentinel = new CancellationTokenSource();
            try
            {
                old.Cancel();
            }
            finally
            {
                old.Dispose(); 
            }        
        }
    }

    public void Dispose()
    {
        _memoryCacheProvider.OnTableChangeTimerElapsed -= OnTableChangeTimerElapsed;
        try
        {
            _sentinel.Cancel();
        }
        finally
        {
            _sentinel.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
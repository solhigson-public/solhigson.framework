using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Solhigson.Framework.Data.Caching;
using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.EfCore;

public class EfCoreCachingSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly List<Type> _changedTypes = [];
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CaptureChangedICachedEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = new CancellationToken())
    {
        CaptureChangedICachedEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        InvalidateCache(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        InvalidateCache(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _changedTypes.Clear();
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData,
        CancellationToken cancellationToken = new CancellationToken())
    {
        _changedTypes.Clear();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void CaptureChangedICachedEntities(DbContext? context)
    {
        if (context is null)
        {
            return;
        }
        var entities = context.ChangeTracker.Entries<ICachedEntity>().ToList();

        foreach (var entry in entities)
        {
            if (entry.State is EntityState.Added or EntityState.Deleted or EntityState.Modified) 
            {
                _changedTypes.Add(entry.Entity.GetType());
            }
        }
    }
    private void InvalidateCache(DbContext? context)
    {
        if (!_changedTypes.HasData())
        {
            return;
        }
        _ = EfCoreCacheManager.InvalidateAsync(_changedTypes.ToArray());
        _changedTypes.Clear();
    }
}
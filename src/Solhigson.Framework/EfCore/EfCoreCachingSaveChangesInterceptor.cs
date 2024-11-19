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

    private static void InvalidateCache(DbContext? context)
    {
        if (context is null)
        {
            return;
        }
        var entities = context.ChangeTracker.Entries<ICachedEntity>().ToList();
        List<Type> types = [];

        foreach (var entry in entities)
        {
            if (entry.State is EntityState.Added or EntityState.Deleted or EntityState.Modified) 
            {
                types.Add(entry.Entity.GetType());
            }
        }

        if (types.HasData())
        {
            _ = EfCoreCacheManager.InvalidateAsync(types.ToArray());
        }
    }
}
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Solhigson.Framework.Extensions;
//
// namespace Solhigson.Framework.Data.Caching;
//
// public class TableChangeTracker : IDisposable
// {
//     public event EventHandler? OnChanged;
//
//     private readonly Dictionary<string, short> _changeIds = new();
//     public TableChangeTracker(IEnumerable<string> tableNames)
//     {
//         foreach (var tableName in tableNames)
//         {
//             _changeIds.Add(tableName, CacheManager.GetTableChangeTrackerId(tableName).Result);
//         }
//         CacheManager.OnTableChangeTimerElapsed += OnTableChangeTimerElapsed;
//     }
//
//     internal string TableNames => CacheManager.Flatten(_changeIds.Keys.ToList());
//         
//     private void OnTableChangeTimerElapsed(object? sender, EventArgs e)
//     {
//         if (e is not ChangeTrackerEventArgs ce)
//         {
//             return;
//         }
//             
//         foreach (var key in _changeIds.Keys)
//         {
//             if (!ce.ChangeIds.TryGetValue(key, out var changeId) || _changeIds[key] == changeId)
//             {
//                 continue;
//             }
//             _changeIds[key] = changeId;
//             this.LogTrace($"Change tracker changed for [{key}]");
//             OnChanged?.Invoke(null, EventArgs.Empty);
//         }
//
//
//         /*
//         if (!ce.ChangeIds.TryGetValue(TableName, out var changeId))
//         {
//             return;
//         }
//         
//         if (changeId == _currentChangeTrackId)
//         {
//             return;
//         }
//         
//         */
//         /*
//         _currentChangeTrackId = changeId;
//         this.ELogDebug($"Change tracker changed for [{TableName}]");
//         OnChanged?.Invoke(null, new EventArgs());
//     */
//     }
//
//     public void Dispose()
//     {
//         CacheManager.OnTableChangeTimerElapsed -= OnTableChangeTimerElapsed;
//         OnChanged = null;
//     }
// }
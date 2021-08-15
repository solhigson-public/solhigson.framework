﻿using System;
using System.Timers;
using Solhigson.Framework.Data.Dto;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Data
{
    public class TableChangeTracker : IDisposable
    {
        private int _currentChangeTrackId;
        internal readonly string TableName;
        public event EventHandler OnChanged;

        public TableChangeTracker(string tableName)
        {
            TableName = tableName;
            _currentChangeTrackId = CacheManager.GetTableChangeTrackerId(tableName).Result;
            CacheManager.OnTableChangeTimerElapsed += OnTableChangeTimerElapsed;
        }
        
        private void OnTableChangeTimerElapsed(object sender, EventArgs e)
        {
            if (!(e is ChangeTrackerEventArgs ce))
            {
                return;
            }

            if (!ce.ChangeIds.TryGetValue(TableName, out var changeId))
            {
                return;
            }
            
            if (changeId == _currentChangeTrackId)
            {
                return;
            }
            
            _currentChangeTrackId = changeId;
            this.ELogDebug($"Change tracker changed for [{TableName}]");
            OnChanged?.Invoke(null, new EventArgs());
        }

        public void Dispose()
        {
            CacheManager.OnTableChangeTimerElapsed -= OnTableChangeTimerElapsed;
            OnChanged = null;
        }
    }
}
﻿using System;
using System.Runtime.Caching;
using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.Data.Caching
{
    public class TableChangeMonitor : ChangeMonitor
    {
        private readonly TableChangeTracker _tableChangeTracker;
        public TableChangeMonitor(TableChangeTracker tableChangeTracker)
        {
            UniqueId = $"{tableChangeTracker.TableNames}_{Guid.NewGuid().ToString()}";
            this.ELogTrace($"New Change monitor: {UniqueId}");
            _tableChangeTracker = tableChangeTracker;
            _tableChangeTracker.OnChanged += TableChangeTrackerOnChanged;
            InitializationComplete();
        }

        private void TableChangeTrackerOnChanged(object sender, EventArgs e)
        {
            this.ELogTrace($"Monitor changed for {UniqueId}");
            OnChanged(UniqueId);
        }

        protected override void Dispose(bool disposing)
        {
            this.ELogTrace($"Dispose called for {UniqueId}");
            _tableChangeTracker.OnChanged -= TableChangeTrackerOnChanged;
        }

        public override string UniqueId { get; }
        
        
    }
}
using System;
using System.Runtime.Caching;
using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.Data.Caching
{
    public class TableChangeMonitor : ChangeMonitor
    {
        private readonly TableChangeTracker _tableChangeTracker;
        public TableChangeMonitor(TableChangeTracker tableChangeTracker)
        {
            var id = $"{tableChangeTracker.TableName}_{Guid.NewGuid().ToString()}";
            UniqueId = id;
            this.ELogDebug($"New Change monitor: {id}");
            _tableChangeTracker = tableChangeTracker;
            _tableChangeTracker.OnChanged += TableChangeTrackerOnChanged;
            InitializationComplete();
        }

        private void TableChangeTrackerOnChanged(object sender, EventArgs e)
        {
            this.ELogDebug($"Monitor changed for {UniqueId}");
            OnChanged(UniqueId);
        }

        protected override void Dispose(bool disposing)
        {
            this.ELogDebug($"Dispose called for {UniqueId}");
            _tableChangeTracker.OnChanged -= TableChangeTrackerOnChanged;
        }

        public override string UniqueId { get; }
        
        
    }
}
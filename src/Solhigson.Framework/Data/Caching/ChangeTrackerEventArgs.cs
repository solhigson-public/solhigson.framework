using System;
using System.Collections.Generic;

namespace Solhigson.Framework.Data.Caching;

public class ChangeTrackerEventArgs : EventArgs
{
    public Dictionary<string, short> ChangeIds { get; }
    public ChangeTrackerEventArgs(Dictionary<string, short> changeIds)
    {
        ChangeIds = changeIds;
    }
}
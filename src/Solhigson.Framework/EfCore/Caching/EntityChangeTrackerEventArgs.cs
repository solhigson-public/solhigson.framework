using System;
using System.Collections.Generic;

namespace Solhigson.Framework.EfCore.Caching;

public class EntityChangeTrackerEventArgs(Dictionary<string, short> changeIds) : EventArgs
{
    public Dictionary<string, short> ChangeIds { get; } = changeIds;
}
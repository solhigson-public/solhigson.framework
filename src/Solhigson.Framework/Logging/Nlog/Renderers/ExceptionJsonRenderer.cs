﻿using System.Text;
using Mapster;
using NLog;
using NLog.LayoutRenderers;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Logging.Nlog.Renderers;

[LayoutRenderer("solhigson-exception")]
public class ExceptionJsonRenderer : LayoutRenderer
{
    protected override void Append(StringBuilder builder, LogEventInfo logEvent)
    {
        if (logEvent?.Exception == null)
        {
            return;
        }

        builder.Append(logEvent.Exception.Adapt<ExceptionInfo>().SerializeToJson());
    }
}
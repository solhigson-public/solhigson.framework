using System;
using System.Text;
using NLog;
using NLog.LayoutRenderers;

namespace Solhigson.Framework.Logging.Nlog.Renderers
{
    [LayoutRenderer("currentDirectory")]
    public class CurrentDirectoryRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            builder.Append(Environment.CurrentDirectory);
        }
    }
}
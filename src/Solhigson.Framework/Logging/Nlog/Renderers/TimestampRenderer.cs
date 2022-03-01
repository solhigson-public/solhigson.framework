using System.Text;
using NLog;
using NLog.LayoutRenderers;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Logging.Nlog.Renderers;

[LayoutRenderer("solhigson-timestamp")]
public class TimestampRenderer : LayoutRenderer
{
    protected override void Append(StringBuilder builder, LogEventInfo logEvent)
    {
        builder.Append(DateUtils.CurrentUnixTimestamp);
    }
}
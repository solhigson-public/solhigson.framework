using System.Text;
using NLog;
using NLog.LayoutRenderers;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Logging.Nlog.Renderers
{
    [LayoutRenderer("fp-exception")]
    public class ExceptionJsonRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            if (logEvent?.Exception == null)
            {
                return;
            }

            builder.Append(logEvent.Exception.SerializeToJson());
        }
    }
}
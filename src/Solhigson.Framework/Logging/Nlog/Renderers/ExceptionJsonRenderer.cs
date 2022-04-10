using System.Text;
using Mapster;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;
using NLog.LayoutRenderers;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Logging.Nlog.Renderers;

[LayoutRenderer("solhigson-exception")]
public class ExceptionJsonRenderer : LayoutRenderer
{
    private static readonly JsonSerializerSettings Settings = new ()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };
    protected override void Append(StringBuilder builder, LogEventInfo logEvent)
    {
        if (logEvent?.Exception == null)
        {
            return;
        }

        builder.Append(logEvent.Exception.Adapt<ExceptionInfo>().SerializeToJson(jsonSerializerSettings: Settings));
    }
}
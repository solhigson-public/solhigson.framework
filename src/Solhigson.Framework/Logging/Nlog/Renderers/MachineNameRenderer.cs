using System.Text;
using NLog;
using NLog.LayoutRenderers;

namespace Solhigson.Framework.Logging.Nlog.Renderers;

[LayoutRenderer("solhigson-machineName")]
public class MachineNameRenderer : LayoutRenderer
{
    public const string Name = "machine-name";

    protected override void Append(StringBuilder builder, LogEventInfo logEvent)
    {
        builder.Append(System.Environment.MachineName);
    }
}
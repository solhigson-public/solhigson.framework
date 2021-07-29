using System.Text;
using NLog;
using NLog.LayoutRenderers;

namespace Solhigson.Framework.Logging.Nlog.Renderers
{
    [LayoutRenderer("fp-group")]
    public class GroupRenderer : LayoutRenderer
    {
        public const string Name = "group";

        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            object group = null;
            logEvent?.Properties?.TryGetValue(Name, out group);

            builder.Append(group ?? Constants.Group.AppLog);
        }
    }
}
using System.Text;
using NLog;
using NLog.LayoutRenderers;

namespace Solhigson.Framework.Logging.Nlog.Renderers
{
    [LayoutRenderer("solhigson-user")]
    public class UserRenderer : LayoutRenderer
    {
        public const string Name = "user";

        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            object user = null;
            logEvent?.Properties?.TryGetValue(Name, out user);

            if (user != null)
            {
                builder.Append(user);
            }
        }
    }
}
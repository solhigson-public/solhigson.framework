using Newtonsoft.Json.Linq;
using NLog;
using NLog.Targets;

namespace Solhigson.Framework.Logging.Nlog.Targets;

public class FormattedJsonFileTarget : FileTarget
{
    protected override string GetFormattedMessage(LogEventInfo logEvent)
    {
        var message = base.GetFormattedMessage(logEvent);
        try
        {
            return JToken.Parse(message).ToString();
        }
        catch
        {
            return message;
        }
    }

}
using NLog.Targets;

namespace Solhigson.Framework.Logging.Nlog.Dto
{
    public class CustomNLogTargetParameters : DefaultNLogParameters
    {
        public CustomNLogTargetParameters(Target customTarget)
        {
            Target = customTarget;
        }
        public Target Target { get; set; }
    }
}
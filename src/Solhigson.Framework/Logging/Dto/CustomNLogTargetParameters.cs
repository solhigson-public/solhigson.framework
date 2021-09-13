using NLog.Targets;

namespace Solhigson.Framework.Logging.Dto
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
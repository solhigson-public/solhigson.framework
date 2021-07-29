using System.Collections.Generic;

namespace Solhigson.Framework.Logging.Dto
{
    public class DefaultNLogParameters
    {
        public DefaultNLogParameters()
        {
            LogLevel = "info";
            LogApiTrace = true;
            ProtectedFields = string.Empty;
        }
        public string LogLevel { get; set; }
        public bool LogApiTrace { get; set; }
        public string ProtectedFields { get; set; }
    }
}
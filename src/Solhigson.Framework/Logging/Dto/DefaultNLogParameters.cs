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
            EncodeChildJsonContent = false;
        }
        
        /// <summary>
        /// Valid values are: "trace", "debug", "info", "warn", "error", "fatal"
        /// Default is "info"
        /// </summary>
        public string LogLevel { get; set; }
        /// <summary>
        /// If this set to [true], LogLevel must be set to at least "info"
        /// </summary>
        public bool LogApiTrace { get; set; }
        public string ProtectedFields { get; set; }
        public bool EncodeChildJsonContent { get; set; }
    }
}
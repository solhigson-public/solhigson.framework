using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;
using NLog.LayoutRenderers;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Logging.Nlog.Renderers
{
    [LayoutRenderer("solhigson-data2")]
    public class CustomDataRenderer2 : LayoutRenderer
    {
        public const string Name = "data";
        private readonly List<string> _protectedFields;
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };


        public CustomDataRenderer2(string protectedFields)
        {
            _protectedFields = new List<string>();

            if (string.IsNullOrWhiteSpace(protectedFields))
            {
                return;
            }

            var split = protectedFields.Split(new[] {',', ';'}, StringSplitOptions.RemoveEmptyEntries);
            _protectedFields.AddRange(split);
        }

        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            object data = null;
            logEvent?.Properties?.TryGetValue(Name, out data);

            if (data == null)
            {
                return;
            }

            var dataType = data.GetType();

            if (dataType.IsClass)
            {
                var jObject = HelperFunctions.CheckForProtectedFields(data, _protectedFields);
                if (jObject != null)
                {
                    data = jObject;
                }
            }

            builder.Append(data.SerializeToJson(jsonSerializerSettings: JsonSerializerSettings));
        }
    }
}
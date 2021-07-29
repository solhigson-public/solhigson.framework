using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NLog.LayoutRenderers;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Logging.Nlog.Renderers
{
    [LayoutRenderer("fp-data")]
    public class CustomDataRenderer : LayoutRenderer
    {
        public const string Name = "data";

        private readonly List<string> _protectedFields;

        public CustomDataRenderer(string protectedFields)
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
            var typeName = string.IsNullOrWhiteSpace(dataType.Namespace)
                ? "Anonymous"
                : dataType.FullName;

            if (dataType.IsClass)
            {
                var jObject = HelperFunctions.CheckForProtectedFields(data, _protectedFields);
                if (jObject != null)
                {
                    data = jObject;
                }
            }

            builder.Append(new {Type = typeName, Data = data}.SerializeToJson());
        }
    }
}
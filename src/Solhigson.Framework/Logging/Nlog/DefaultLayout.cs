using NLog.Layouts;

namespace Solhigson.Framework.Logging.Nlog
{
    public static class DefaultLayout
    {
        public static JsonLayout GetDefaultJsonLayout(bool encodeChildJsonContent = true)
        {
            return new JsonLayout
            {
                Attributes =
                {

                    new JsonAttribute("Date", "${longdate}", true),
                    new JsonAttribute("LogLevel", "${level}", true),
                    new JsonAttribute("Description", "${message}", true),
                    new JsonAttribute("Source", "${logger}", true),
                    new JsonAttribute("ServiceName", "${event-properties:item=serviceName}", true),
                    new JsonAttribute("ServiceType", "${event-properties:item=serviceType}", true),
                    new JsonAttribute("ServiceUrl", "${event-properties:item=url}", true),
                    new JsonAttribute("Status", "${event-properties:item=status}", true),

                    new JsonAttribute("Group", "${solhigson-group}", true),
                    new JsonAttribute("Exception", "${solhigson-exception}", encodeChildJsonContent),
                    new JsonAttribute("Data", "${solhigson-data}", encodeChildJsonContent),
                    new JsonAttribute("User", "${solhigson-user}", true),


                    new JsonAttribute("MachineName", "${machineName}", true)
                },

            };
        }

        public static JsonLayout TestsLayout =>
            new JsonLayout
            {
                Attributes =
                {
                    new JsonAttribute("LogLevel", "${level}", true),
                    new JsonAttribute("Description", "${message}", true),
                    new JsonAttribute("Source", "${logger}", true),

                    new JsonAttribute("Exception", "${solhigson-exception}", false),
                    new JsonAttribute("Data", "${solhigson-data}", false),
                    new JsonAttribute("Data", "${solhigson-data}", false),
                    new JsonAttribute("User", "${solhigson-user}", true),
                },

            };
    }
}
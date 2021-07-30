using NLog.Layouts;

namespace Solhigson.Framework.Logging.Nlog
{
    public static class DefaultLayout
    {
        public static readonly JsonLayout Layout = new JsonLayout
        {
            Attributes =
            {
                new JsonAttribute("ServiceName", "${event-properties:item=serviceName}", true),
                new JsonAttribute("ServiceType", "${event-properties:item=serviceType}", true),
                new JsonAttribute("ServiceUrl", "${event-properties:item=url}", true),
                new JsonAttribute("Status", "${event-properties:item=status}", true),

                new JsonAttribute("Group", "${solhigson-group}", true),
                new JsonAttribute("Exception", "${solhigson-exception}", true),
                new JsonAttribute("Data", "${solhigson-data}", true),
                new JsonAttribute("User", "${solhigson-user}", true),

                new JsonAttribute("LogLevel", "${level}", true),
                new JsonAttribute("Description", "${message}", true),
                new JsonAttribute("Source", "${logger}", true),

                new JsonAttribute("MachineName", "${machineName}", true)
            }
        };
    }
}
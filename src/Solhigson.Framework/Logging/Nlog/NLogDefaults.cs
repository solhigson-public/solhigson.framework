using System;
using NLog.Layouts;
using NLog.Targets;
using Solhigson.Framework.Logging.Nlog.Targets;

namespace Solhigson.Framework.Logging.Nlog;

public static class NLogDefaults
{
    public static Target GetDefaultFileTarget(bool encodeChildJsonContent = true, bool isFallBack = false)
    {
        return new FormattedJsonFileTarget
        {
            FileName = $"{Environment.CurrentDirectory}/log.log",
            Name = isFallBack ? "FileFallback" : "FileDefault",
            ArchiveAboveSize = 2560000,
            ArchiveNumbering = ArchiveNumberingMode.Sequence,
            Layout = GetDefaultJsonLayout(encodeChildJsonContent)
        };
    }

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
                new JsonAttribute("ChainId", "${event-properties:item=chainId}", true),

                new JsonAttribute("Group", "${solhigson-group}", true),
                new JsonAttribute("Exception", "${solhigson-exception}", encodeChildJsonContent),
                new JsonAttribute("Data", "${solhigson-data}", encodeChildJsonContent),
                new JsonAttribute("User", "${solhigson-user}", true),


                new JsonAttribute("MachineName", "${solhigson-machineName}", true)
            },

        };
    }
        
    public static JsonLayout GetDefaultJsonLayout2(bool encodeChildJsonContent = true)
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
                new JsonAttribute("ChainId", "${event-properties:item=chainId}", true),

                new JsonAttribute("Group", "${solhigson-group}", true),
                new JsonAttribute("Exception", "${solhigson-exception}", encodeChildJsonContent),
                new JsonAttribute("Data", "${solhigson-data2}", encodeChildJsonContent),
                new JsonAttribute("User", "${solhigson-user}", true),


                new JsonAttribute("MachineName", "${solhigson-machineName}", true)
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
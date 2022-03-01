using System;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Targets;
using Xunit.Abstractions;

namespace Solhigson.Framework.Logging.Nlog.Targets;

public class XUnitTestOutputHelperTarget : TargetWithLayout
{
    private readonly ITestOutputHelper _outputHelper;

    public XUnitTestOutputHelperTarget(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    protected override void Write(LogEventInfo logEvent)
    {
        var output = "";
        try
        {
            output = Layout.Render(logEvent);
            _outputHelper.WriteLine(JToken.Parse(output).ToString());
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine($"{output} Error Rendering: {ex.Message}");
        }
    }
}
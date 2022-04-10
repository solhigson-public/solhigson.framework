using System;

namespace Solhigson.Framework.Logging;

public record ExceptionInfo
{
    public string Message { get; set; }
    public string StackTrace { get; set; }
    public ExceptionInfo InnerException { get; set; }
}
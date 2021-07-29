using Microsoft.AspNetCore.Mvc;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Web.Filters;

namespace Solhigson.Framework.Web
{
    [ApiController]
    [ApiExceptionFilter]
    public class ApiControllerBase : ControllerBase
    {
        internal ApiTraceData TraceData { get; set; }
        public SolhigsonServicesWrapper SolhigsonServicesWrapper { get; set; }
    }
}
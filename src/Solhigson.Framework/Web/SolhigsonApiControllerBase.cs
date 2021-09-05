using Microsoft.AspNetCore.Mvc;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Web
{
    [ApiController]
    public class SolhigsonApiControllerBase : ControllerBase
    {
        public SolhigsonServicesWrapper SolhigsonServicesWrapper { get; set; }
    }
}
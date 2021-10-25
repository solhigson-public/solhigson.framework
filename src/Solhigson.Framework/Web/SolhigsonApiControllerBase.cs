using Microsoft.AspNetCore.Mvc;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Web.Attributes;

namespace Solhigson.Framework.Web
{
    [ApiController]
    [SolhigsonModelValidation]
    public class SolhigsonApiControllerBase : ControllerBase
    {
        
    }
}
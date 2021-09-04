using Microsoft.AspNetCore.Mvc;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Web
{
    public class SolhigsonMvcControllerBase : Controller
    {
        public SolhigsonServicesWrapper SolhigsonServicesWrapper { get; set; }
    }
}
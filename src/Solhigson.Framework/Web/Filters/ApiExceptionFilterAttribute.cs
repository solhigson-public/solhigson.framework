using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Web.Filters
{
    public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext actionExecutedContext)
        {
            this.ELogError(actionExecutedContext.Exception, "Caught by Api Exception Filter");

            var respObj = new ResponseInfo
            {
                StatusCode = StatusCode.UnExpectedError,
                Message = "Internal Server Error"
            };

            /*
            if (!(actionExecutedContext.ActionDescriptor is ControllerActionDescriptor c)) return;

            var lifetimeScope = actionExecutedContext.HttpContext.RequestServices.GetService<ILifetimeScope>();

            if (!(lifetimeScope?.Resolve(c.ControllerTypeInfo) is ApiControllerBase controller)) return;
            */

            actionExecutedContext.Result = new ObjectResult(respObj)
            {
                StatusCode = (int) HttpStatusCode.InternalServerError
            };
        }
    }
}
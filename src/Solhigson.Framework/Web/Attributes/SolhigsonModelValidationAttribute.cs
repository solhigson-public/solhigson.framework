using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Web.Attributes
{
    public class SolhigsonModelValidationAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.Controller is not ControllerBase cont)
            {
                return;
            }

            if (cont.ModelState.IsValid)
            {
                return;
            }
            
            if (!context.HttpContext.IsApiController())
            {
                return;
            }

            var error = new StringBuilder();
            foreach (var modelError in cont.ModelState.Values.SelectMany(model => model.Errors))
            {
                error.AppendLine(modelError.ErrorMessage);
            }

            context.Result = new JsonResult(ResponseInfo.FailedResult(error.ToString()))
            {
                StatusCode = StatusCodes.Status200OK
            };

        }

        public void OnActionExecuted(ActionExecutedContext context)
        {

        }
    }
}
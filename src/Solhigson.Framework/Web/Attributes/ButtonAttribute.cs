using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;

namespace Solhigson.Framework.Web.Attributes;

public class ButtonAttribute : ActionMethodSelectorAttribute
{
    public string ButtonName { get; set; }

    public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action)
    {
        return routeContext.HttpContext.Request.Form.ContainsKey(ButtonName);
    }

    public ButtonAttribute(string name)
    {
        ButtonName = name;
    }
}
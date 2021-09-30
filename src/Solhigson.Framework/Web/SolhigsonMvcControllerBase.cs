using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Solhigson.Framework.Dto;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Web
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SolhigsonMvcControllerBase : Controller
    {
        protected bool IsChecked(string name)
        {
            if (!HttpMethods.IsPost(Request.Method))
            {
                return false;
            }
            var isChecked = Request.Form[name];
            return isChecked.Any(t => string.Compare("on", t, StringComparison.OrdinalIgnoreCase) == 0);
        }
        
        protected string IsChecked(bool value)
        {
            return value ? "checked" : "";
        }

        protected void SetMessage(string message, bool isError)
        {
            var messageType = isError ? PageMessageType.Error : PageMessageType.Info;
            this.SetDisplayMessage(message, messageType);
        }
        
        protected void SetErrorMessage(string message, bool closeOnClick = true, bool encodeHtml = true)
        {
            this.SetDisplayMessage(message, PageMessageType.Error, closeOnClick, encodeHtml: encodeHtml);
        }

        protected void SetInfoMessage(string message, bool closeOnClick = true, bool clearBeforeAdd = false, bool encodeHtml = true)
        {
            this.SetDisplayMessage(message, PageMessageType.Info, closeOnClick, clearBeforeAdd, encodeHtml);
        }

        protected int TryGetRequestParameterAsInteger(string name, int defaultValue)
        {
            var vals = StringValues.Empty;
            if (HttpMethods.IsPost(Request.Method))
            {
                vals = Request.Form[name];
            }

            if (!vals.Any())
            {
                vals = Request.Query[name];
            }
            if (!vals.Any())
            {
                return defaultValue;
            }
            var requestValue = vals.FirstOrDefault(); ;
            return int.TryParse(requestValue, out int tryValue) ? tryValue : defaultValue;
        }

        protected int TryGetPageSize(int defaultValue = 20)
        {
            return TryGetRequestParameterAsInteger("grid-pageSize", defaultValue);
        }

        protected int TryGetPageIndex(int defaultValue = 1)
        {
            return TryGetRequestParameterAsInteger("grid-page", defaultValue);
        }

        protected int PageSize => TryGetPageSize();

        protected int CurrentPage => TryGetPageIndex();
        
        protected static int GetTimeZoneOffset()
        {
            return LocaleUtil.GetTimeZoneOffset();
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (HttpContext.Request.Cookies.ContainsKey(Constants.TimeZoneCookieName))
            {
                HelperFunctions.SafeSetSessionData(Constants.TimeZoneCookieName,
                    HttpContext.Request.Cookies[Constants.TimeZoneCookieName], HttpContext);
            }
            base.OnActionExecuting(filterContext);
        }

        protected int GetPage()
        {
            if (HttpMethods.IsPost(Request.Method)) return 1;
            var page = Request.Query[Constants.PaginationPage];
            return int.TryParse(page, out var pageVal) ? pageVal : 1;
        }
        
        public void AddPaginationParameters(PagedSearchParameters parameters)
        {
            TempData[Constants.PaginationParameters] = parameters?.SerializeToJson();
        }

    }
}
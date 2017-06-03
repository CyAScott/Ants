using System;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

#pragma warning disable 1591

namespace Ants.Web.Filters
{
    public class IdFilter : ActionFilterAttribute
    {
        public const string FilterResults = "Filter-Results";
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var id = (Guid?)HttpContext.Current.Items["id"];

            if (id == null || id.Value == Guid.Empty)
            {
                HttpContext.Current.Response.Headers.Add(FilterResults, "false");
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            HttpContext.Current.Response.AddHeader(FilterResults, "true");
        }
    }
}
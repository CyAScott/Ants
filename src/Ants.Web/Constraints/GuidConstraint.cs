using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using System.Web.Http.Routing;

#pragma warning disable 1591

namespace Ants.Web.Constraints
{
    public class GuidConstraint : IHttpRouteConstraint
    {
        public const string ConstraintResults = "Constraint-Results";
        public bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName, IDictionary<string, object> values, HttpRouteDirection routeDirection)
        {
            if (!values.TryGetValue(parameterName, out object value) || value == null || !Guid.TryParse(value.ToString(), out Guid id))
            {
                HttpContext.Current.Response.Headers.Add(ConstraintResults, "false");
                return false;
            }

            HttpContext.Current.Items["id"] = id;
            HttpContext.Current.Response.AddHeader(ConstraintResults, "true");

            return true;
        }
    }
}
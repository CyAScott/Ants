using System.Web.Http;
using System.Web.Http.Routing;
using Ants.Web.Constraints;

#pragma warning disable 1591

namespace Ants.Web
{
    public class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            var resolver = new DefaultInlineConstraintResolver();
            resolver.ConstraintMap.Add("guidItem", typeof(GuidConstraint));

            // Web API routes
            config.MapHttpAttributeRoutes(resolver);

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
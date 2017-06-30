using System.Web;
using System.Web.Routing;

namespace Ants.Owin
{
    /// <summary>
    /// A request context wrapper for the <see cref="RequestContext"/>.
    /// </summary>
    public class AntsRequestContext : RequestContext
    {
        /// <summary>
        /// Create a request context wrapper for the <see cref="RequestContext"/>.
        /// </summary>
        public AntsRequestContext(HttpContext context, RouteData routeData = null)
            : base(new AntsHttpContextWrapper(context), routeData ?? new RouteData())
        {
        }
    }
}

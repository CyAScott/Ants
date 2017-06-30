using Ants.Web;
using Microsoft.Owin;
using Owin;

#pragma warning disable 1591

[assembly: OwinStartup(typeof(Startup))]

namespace Ants.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
        }
    }
}
using System;
using Ants;
using Ants.Owin;
using Microsoft.Owin.Host.SystemWeb;

#pragma warning disable 1591

[assembly: AutoLoadIntoAnts(LoadAfterOtherAssemblies = true, AutoLoadAssemblyHelper = typeof(AntsForOwinHelper))]

namespace Ants.Owin
{
    internal class AntsForOwinHelper : AutoLoadAssemblyHelper
    {
        public static readonly Type OwinHttpModuleType = typeof(OwinHttpHandler).Assembly.GetType("Microsoft.Owin.Host.SystemWeb.OwinHttpModule");

        public override void BeforeFirstRouteLoad()
        {
            //ReplaceHttpModules(OwinHttpModuleType, typeof(AntsForOwinHttpModule));
        }
    }
}

using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

#pragma warning disable 1591

namespace Ants.Web
{
    public class Global : HttpApplication
    {
        public static NameValueCollection AppSettings = ConfigurationManager.AppSettings;

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
#if DEBUG
            Testing.Variables.ApplicationAuthenticateRequestCalled = true;
#endif
        }
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
#if DEBUG
            Testing.Variables.ApplicationBeginRequestCalled = true;
#endif
        }
        protected void Application_End(object sender, EventArgs e)
        {
#if DEBUG
            Testing.Variables.ApplicationEndCalled = true;
#endif
        }
        protected void Application_Error(object sender, EventArgs e)
        {
#if DEBUG
            Testing.Variables.ApplicationErrorCalled = true;
#endif
        }
        protected void Application_Start(object sender, EventArgs e)
        {
#if DEBUG
            Testing.Variables.ApplicationStartCalled = true;
#endif
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
#if DEBUG
            Testing.Variables.ApplicationStartCompleted = true;
#endif
        }
        protected void Session_End(object sender, EventArgs e)
        {
#if DEBUG
            Testing.Variables.SessionEndCalled = true;
#endif
        }
        protected void Session_Start(object sender, EventArgs e)
        {
#if DEBUG
            Testing.Variables.SessionStartCalled = true;
#endif
        }
    }
}
using System.Web.Optimization;

#pragma warning disable 1591

namespace Ants.Web
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/js").Include(
                "~/Scripts/jquery-1.10.2.js",
                "~/Scripts/bootstrap.js"
            ));
        }
    }
}
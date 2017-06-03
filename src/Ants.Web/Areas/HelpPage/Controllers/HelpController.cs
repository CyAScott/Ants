using System.Web.Http;
using System.Web.Mvc;
using Ants.Web.Areas.HelpPage.ModelDescriptions;

#pragma warning disable 1591

namespace Ants.Web.Areas.HelpPage.Controllers
{
    /// <summary>
    /// The controller that will handle requests for the help page.
    /// </summary>
    public class HelpController : Controller
    {
        private const string errorViewName = "Error";

        public HelpController()
            : this(GlobalConfiguration.Configuration)
        {
        }

        public HelpController(HttpConfiguration config)
        {
            Configuration = config;
        }

        public HttpConfiguration Configuration { get; }

        public ActionResult Index()
        {
            ViewBag.DocumentationProvider = Configuration.Services.GetDocumentationProvider();
            return View(Configuration.Services.GetApiExplorer().ApiDescriptions);
        }

        public ActionResult Api(string apiId)
        {
            if (!string.IsNullOrEmpty(apiId))
            {
                var apiModel = Configuration.GetHelpPageApiModel(apiId);
                if (apiModel != null)
                {
                    return View(apiModel);
                }
            }

            return View(errorViewName);
        }

        public ActionResult ResourceModel(string modelName)
        {
            if (!string.IsNullOrEmpty(modelName))
            {
                var modelDescriptionGenerator = Configuration.GetModelDescriptionGenerator();
                if (modelDescriptionGenerator.GeneratedModels.TryGetValue(modelName, out ModelDescription modelDescription))
                {
                    return View(modelDescription);
                }
            }

            return View(errorViewName);
        }
    }
}
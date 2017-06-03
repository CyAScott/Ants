using System;
using System.Web.Mvc;
using Ants.Web.Models;

namespace Ants.Web.Controllers
{
    /// <summary>
    /// Example MVC controller.
    /// </summary>
    public class TestMvcController : Controller
    {
        /// <summary>
        /// A simple MVC post.
        /// </summary>
        [HttpGet]
        public ActionResult Index()
        {
            return View((MvcResponse)null);
        }

        /// <summary>
        /// A simple MVC post.
        /// </summary>
        [HttpPost]
        public ActionResult Index(MvcRequest request)
        {
            return View("Index", new MvcResponse
            {
                CreatedOn = DateTime.UtcNow,
                Id = request.Id,
                Name = request.Name
            });
        }
    }
}
using System.Web.Http;
using Ants.Web.Filters;

namespace Ants.Web.Controllers
{
    /// <summary>
    /// Example controller for testing query string.
    /// </summary>
    public class TestQueryStringController : ApiController
    {
        /// <summary>
        /// A simple get by ID with a query string test.
        /// </summary>
        [IdFilter, HttpGet, Route("api/TestQueryString/{id:guidItem}")]
        public string TestQueryString(string id)
        {
            return Request.RequestUri.Query;
        }
    }
}
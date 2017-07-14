using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace Ants.Web.Controllers
{
    /// <summary>
    /// Example controller for testing cookies.
    /// </summary>
    public class TestCookiesController : ApiController
    {
        /// <summary>
        /// A simple get by ID with a query string test.
        /// </summary>
        [HttpPost, Route("api/TestCookies")]
        public HttpResponseMessage TestCookies([FromBody]string[] cookieValues)
        {
            var returnValue = new HttpResponseMessage(HttpStatusCode.OK);

            returnValue.Headers.AddCookies(cookieValues
                .Select(value => CookieHeaderValue.TryParse(value, out CookieHeaderValue cookie) ? cookie : null)
                .Where(cookie => cookie != null));

            return returnValue;
        }
    }
}
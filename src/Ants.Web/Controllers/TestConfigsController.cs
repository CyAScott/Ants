using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Ants.Web.Controllers
{
    /// <summary>
    /// API controller for reading config settings.
    /// </summary>
    public class TestConfigsController : ApiController
    {
        /// <summary>
        /// Gets the AppSettings.
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("api/Configs/AppSettings")]
        public Dictionary<string, string> Get()
        {
            return Global.AppSettings
                .AllKeys
                .ToDictionary(key => key, key => Global.AppSettings[key]);
        }
    }
}
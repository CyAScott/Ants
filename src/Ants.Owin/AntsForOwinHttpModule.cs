using System.Web;

namespace Ants.Owin
{
    /// <summary>
    /// Makes OWIN handle requests from Ants instead of IIS 7.
    /// </summary>
    public sealed class AntsForOwinHttpModule : IHttpModule
    {
        /// <inheritdoc />
        public void Init(HttpApplication context)
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            
        }
    }
}

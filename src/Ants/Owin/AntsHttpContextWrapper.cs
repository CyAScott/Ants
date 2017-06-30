using System.Reflection;
using System.Web;
using Ants.HttpRequestQueue;

namespace Ants.Owin
{
    /// <summary>
    /// A context wrapper for the <see cref="HttpContext"/>.
    /// </summary>
    public class AntsHttpContextWrapper : HttpContextWrapper
    {
        /// <summary>
        /// Creates a context wrapper for the <see cref="HttpContext"/>.
        /// </summary>
        public AntsHttpContextWrapper(HttpContext context)
            : base(context)
        {
            Context = context;
            HttpWorkerRequestMessage = typeof(HttpContext)
                .GetField("_wr", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(context) as HttpWorkerRequestMessage;
        }

        internal HttpContext Context { get; }
        internal HttpWorkerRequestMessage HttpWorkerRequestMessage { get; }

        /// <inheritdoc />
        public override HttpRequestBase Request => new AntsHttpRequestWrapper(this);

        /// <inheritdoc />
        public override HttpResponseBase Response => new AntsHttpResponseWrapper(this);
    }
}

using System.Collections.Specialized;
using System.IO;
using System.Web;

namespace Ants.Owin
{
    /// <summary>
    /// A request wrapper for the <see cref="HttpRequest"/>.
    /// </summary>
    public class AntsHttpRequestWrapper : HttpRequestWrapper
    {
        /// <summary>
        /// Creates a request wrapper for the <see cref="HttpRequest"/>.
        /// </summary>
        public AntsHttpRequestWrapper(AntsHttpContextWrapper parent)
            : base(parent.Context.Request)
        {
            Headers = parent.Context.Request.Headers;
            InputStream = parent.HttpWorkerRequestMessage.Message.RequestStream;
        }

        /// <inheritdoc />
        public override NameValueCollection Headers { get; }

        /// <inheritdoc />
        public override Stream InputStream { get; }
    }
}

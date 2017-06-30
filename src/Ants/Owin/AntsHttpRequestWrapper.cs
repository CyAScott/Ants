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
            var headers = Headers = new NameValueCollection();
            foreach (var header in parent.HttpWorkerRequestMessage.RequestHeaders)
            {
                headers[header.Key] = header.Value;
            }

            InputStream = parent.HttpWorkerRequestMessage.Message.RequestStream;
        }

        /// <inheritdoc />
        public override NameValueCollection Headers { get; }

        /// <inheritdoc />
        public override Stream InputStream { get; }
    }
}

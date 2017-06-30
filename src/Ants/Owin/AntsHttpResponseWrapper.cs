using System.Collections.Specialized;
using System.Web;

namespace Ants.Owin
{
    /// <summary>
    /// A response wrapper for the <see cref="HttpResponse"/>.
    /// </summary>
    public class AntsHttpResponseWrapper : HttpResponseWrapper
    {
        private readonly AntsHttpContextWrapper parent;

        /// <summary>
        /// Creates a response wrapper for the <see cref="HttpResponse"/>.
        /// </summary>
        public AntsHttpResponseWrapper(AntsHttpContextWrapper parent)
            : base(parent.Context.Response)
        {
            this.parent = parent;
        }

        /// <inheritdoc />
        public override NameValueCollection Headers
        {
            get
            {
                var headers = new NameValueCollection();
                foreach (var header in parent.HttpWorkerRequestMessage.ResponseHeaders)
                {
                    headers[header.Key] = string.Join("; ", header.Value);
                }
                return headers;
            }
        }

        /// <inheritdoc />
        public override void AppendHeader(string name, string value)
        {
            parent.HttpWorkerRequestMessage.SendUnknownResponseHeader(name, value);
        }

        /// <inheritdoc />
        public override void ClearHeaders()
        {
            parent.HttpWorkerRequestMessage.Message.ClearHeaders();
        }
    }
}

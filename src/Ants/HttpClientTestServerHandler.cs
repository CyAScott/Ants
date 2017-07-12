using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ants.HttpRequestQueue;

namespace Ants
{
    /// <summary>
    /// A HTTP handler that converts <see cref="HttpRequestMessage"/>s to <see cref="HttpResponseMessage"/>s by processing the messages via Ants.
    /// </summary>
    public class HttpClientTestServerHandler : HttpClientHandler
    {
        //// ReSharper disable once InconsistentNaming
        //private readonly Action CheckDisposed;
        //// ReSharper disable once InconsistentNaming
        //private readonly Action SetOperationStarted;
        //// ReSharper disable once InconsistentNaming
        //private readonly Action<HttpResponseMessage> ProcessResponse;
        //// ReSharper disable once InconsistentNaming
        //private readonly Func<HttpRequestMessage, Task> ConfigureRequest;

        private static HttpResponseMessage createBadGatewayResponseMessage(bool serverExists)
        {
            return new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                ReasonPhrase = serverExists ? "Server Not Ready" : "Connection Failed"
            };
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var domain = request.RequestUri.Host;
            if (!request.RequestUri.IsDefaultPort)
            {
                domain += $":{request.RequestUri.Port}";
            }

            //await ConfigureRequest(request).ConfigureAwait(false);

            var message = new Message(request, request.Content == null ? null : await request.Content.ReadAsStreamAsync().ConfigureAwait(false));

            //CheckDisposed();
            //SetOperationStarted();

            HttpResponseMessage response;
            switch (AspNetTestServer.DefaultDomainWorker.Enqueue(domain, message))
            {
                case EnqueueResults.ApplicationNotFound:
                    response = createBadGatewayResponseMessage(serverExists: false);
                    break;
                case EnqueueResults.ApplicationNotInitialize:
                    response = createBadGatewayResponseMessage(serverExists: true);
                    break;
                case EnqueueResults.Enqueued:
                    response = await message.Task.Task.ConfigureAwait(false);
                    break;
                default:
                    throw new NotSupportedException();
            }

            //ProcessResponse(response);

            return response;
        }

        /// <summary>
        /// Creates a HTTP handler that converts <see cref="HttpRequestMessage"/>s to <see cref="HttpResponseMessage"/>s by processing the messages via Ants.
        /// </summary>
        public HttpClientTestServerHandler()
        {
            //CheckDisposed = (Action)typeof(HttpClientHandler)
            //    .GetMethod(nameof(CheckDisposed), BindingFlags.Instance | BindingFlags.NonPublic)
            //    .CreateDelegate(typeof(Action), this);
            //SetOperationStarted = (Action)typeof(HttpClientHandler)
            //    .GetMethod(nameof(SetOperationStarted), BindingFlags.Instance | BindingFlags.NonPublic)
            //    .CreateDelegate(typeof(Action), this);
            
            CookieContainer = new CookieContainer();
            UseCookies = true;
        }
    }
}

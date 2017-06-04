using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ants
{
    internal class HttpClientTestServerHandler : HttpMessageHandler
    {
        private static HttpResponseMessage createBadGatewayResponseMessage(bool serverExists)
        {
            return new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                ReasonPhrase = serverExists ? "Server Not Ready" : "Connection Failed"
            };
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var domain = request.RequestUri.Host;
            if (!request.RequestUri.IsDefaultPort)
            {
                domain += $":{request.RequestUri.Port}";
            }

            var message = new Message(request, request.Content == null ? null : await request.Content.ReadAsStreamAsync().ConfigureAwait(false));

            switch (AspNetTestServer.DefaultDomainWorker.Enqueue(domain, message))
            {
                case EnqueueResults.ApplicationNotFound:
                    return createBadGatewayResponseMessage(serverExists: false);
                case EnqueueResults.ApplicationNotInitialize:
                    return createBadGatewayResponseMessage(serverExists: true);
                case EnqueueResults.Enqueued:
                    return await message.Task.Task.ConfigureAwait(false);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

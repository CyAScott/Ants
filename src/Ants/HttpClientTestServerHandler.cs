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
            var key = request.RequestUri.Host;
            if (!request.RequestUri.IsDefaultPort)
            {
                key += $":{request.RequestUri.Port}";
            }

            if (!AspNetTestServer.Applications.TryGetValue(key, out HttpApplicationRequestQueue applicationRequestQueue))
            {
                return createBadGatewayResponseMessage(serverExists: false);
            }

            if (applicationRequestQueue == null)
            {
                return createBadGatewayResponseMessage(serverExists: true);
            }

            var message = new Message(request, request.Content == null ? null : await request.Content.ReadAsStreamAsync().ConfigureAwait(false));

            applicationRequestQueue.Enqueue(message);

            return await message.Task.Task.ConfigureAwait(false);
        }
    }
}

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ants.HttpRequestQueue;

namespace Ants
{
    /// <summary>
    /// A HTTP handler that converts <see cref="HttpRequestMessage"/>s to <see cref="HttpResponseMessage"/>s by processing the messages via Ants.
    /// </summary>
    public class HttpClientTestServerHandler : HttpMessageHandler
    {
        private static HttpResponseMessage createBadGatewayResponseMessage(bool serverExists)
        {
            return new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                ReasonPhrase = serverExists ? "Server Not Ready" : "Connection Failed"
            };
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var domain = request.RequestUri.Host;
            if (!request.RequestUri.IsDefaultPort)
            {
                domain += $":{request.RequestUri.Port}";
            }

            var cookiesEnabled = CookiesEnabled;
            if (cookiesEnabled)
            {
                var cookieHeader = Cookies.GetCookieHeader(request.RequestUri);
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    request.Headers.Add("Cookie", cookieHeader.Split(';').Select(item => item.Trim()));
                }
            }

            var message = new Message(request,
                request.Content == null ? null : await request.Content.ReadAsStreamAsync().ConfigureAwait(false));

            switch (AspNetTestServer.DefaultDomainWorker.Enqueue(domain, message))
            {
                case EnqueueResults.ApplicationNotFound:
                    return createBadGatewayResponseMessage(serverExists: false);
                case EnqueueResults.ApplicationNotInitialize:
                    return createBadGatewayResponseMessage(serverExists: true);
                case EnqueueResults.Enqueued:
                    break;
                default:
                    throw new NotSupportedException();
            }

            var response = await message.Task.Task.ConfigureAwait(false);

            if (!cookiesEnabled)
            {
                return response.Item1;
            }

            lock (Cookies)
            {
                foreach (var cookie in response.Item2)
                {
                    Cookies.SetCookies(request.RequestUri, cookie);
                }
            }

            return response.Item1;
        }

        /// <summary>
        /// The cookie collection.
        /// </summary>
        public CookieContainer Cookies { get; private set; } = new CookieContainer();

        /// <summary>
        /// Enables/disables the cookies. Cookies are enabled by default.
        /// </summary>
        public bool CookiesEnabled { get; set; } = true;

        /// <summary>
        /// Removes all stored cookies.
        /// </summary>
        public void ClearCookies()
        {
            Cookies = new CookieContainer();
        }
    }
}

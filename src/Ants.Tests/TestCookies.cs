using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Ants.Web;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Ants.Tests
{
    [TestFixture]
    public class TestCookies
    {
        [Test]
        public async Task Test()
        {
            TestHelper.EnsureNothingIsRunning();

            AspNetTestServer.Start<Global>(new StartApplicationArgs
            {
                PhysicalDirectory = TestHelper.WebProjectDirectory
            });

            TestHelper.EnsureServerStarted();

            using (var client = AspNetTestServer.GetHttpClient<Global>())
            {
                const string testUrl = "/api/TestCookies";
                var ids = new []
                {
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString()
                };
                var now = DateTime.UtcNow;

                var cookieValues = new[]
                {
                    $"value1={ids[0]}; Expires={now.AddHours(1):ddd, d MMM yyyy H:m:s} GMT; HttpOnly",
                    $"value2={ids[1]}; Expires={now.AddHours(2):ddd, d MMM yyyy H:m:s} GMT",
                    $"value3={ids[2]}; HttpOnly"
                };
                var requestCookies = cookieValues
                    .Select(value => CookieHeaderValue.TryParse(value, out CookieHeaderValue cookie) ? cookie : null)
                    .Where(cookie => cookie != null)
                    .ToArray();
                using (var content = new StringContent(JsonConvert.SerializeObject(cookieValues), Encoding.UTF8, "application/json"))
                using (var request = new HttpRequestMessage(HttpMethod.Post, testUrl)
                {
                    Content = content
                })
                {
                    var url = new Uri($"http://{client.BaseAddress.Host}{testUrl}");
                    var currentCookies = AspNetTestServer.HttpMessageHandler.Cookies.GetCookies(url);
                    Assert.IsNotNull(currentCookies);
                    Assert.AreEqual(currentCookies.Count, 0);

                    using (var result = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        result.EnsureSuccessStatusCode();

                        var responseCookies = AspNetTestServer.HttpMessageHandler.Cookies.GetCookies(url);
                        Assert.IsNotNull(responseCookies);
                        Assert.AreEqual(responseCookies.Count, requestCookies.Length);

                        foreach (var responseCookie in responseCookies.Cast<Cookie>())
                        {
                            var requestCookie = requestCookies
                                .Select(cookie => cookie.Cookies.FirstOrDefault(item => item.Value == responseCookie.Value))
                                .FirstOrDefault(cookie => cookie != null);

                            Assert.IsNotNull(requestCookie);

                            Assert.AreEqual(requestCookie.Name, responseCookie.Name);
                        }
                    }
                }
            }

            await AspNetTestServer.Stop<Global>().ConfigureAwait(false);

            TestHelper.EnsureServerStopped();
        }
    }
}

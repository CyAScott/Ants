using System;
using System.Linq;
using System.Net;
using System.Net.Http;
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
                using (var content = new StringContent(JsonConvert.SerializeObject(cookieValues), Encoding.UTF8, "application/json"))
                using (var request = new HttpRequestMessage(HttpMethod.Post, "/api/TestCookies")
                {
                    Content = content
                })
                using (var result = await client.SendAsync(request).ConfigureAwait(false))
                {
                    result.EnsureSuccessStatusCode();

                    var cookies = AspNetTestServer.HttpMessageHandler.Cookies.GetCookies(request.RequestUri);
                    Assert.IsNotNull(cookies);
                    Assert.AreEqual(cookies.Count, 3);
                    Assert.IsTrue(cookies
                        .Cast<Cookie>()
                        .All(cookie => ids.Contains(cookie.Value)));
                }
            }

            await AspNetTestServer.Stop<Global>().ConfigureAwait(false);

            TestHelper.EnsureServerStopped();
        }
    }
}

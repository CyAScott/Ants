using System;
using System.Threading.Tasks;
using Ants.Web;
using HtmlAgilityPack;
using NUnit.Framework;

namespace Ants.Tests
{
    [TestFixture]
    public class TestStaticsContent
    {
        [Test]
        public async Task TestBundleFile()
        {
            TestHelper.EnsureNothingIsRunning();

            AspNetTestServer.Start<Global>(new StartApplicationArgs
            {
                PhysicalDirectory = TestHelper.WebProjectDirectory
            });

            TestHelper.EnsureServerStarted();

            using (var client = AspNetTestServer.GetHttpClient<Global>())
            using (var response = await client.GetAsync("/bundles/js").ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var js = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Assert.IsNotNull(js);
            }

            await AspNetTestServer.Stop<Global>().ConfigureAwait(false);

            TestHelper.EnsureServerStopped();
        }

        [Test]
        public async Task TestStaticFile()
        {
            TestHelper.EnsureNothingIsRunning();

            AspNetTestServer.Start<Global>(new StartApplicationArgs
            {
                PhysicalDirectory = TestHelper.WebProjectDirectory
            });

            TestHelper.EnsureServerStarted();

            using (var client = AspNetTestServer.GetHttpClient<Global>())
            using (var response = await client.GetAsync("/StaticPage.html").ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Assert.IsNotNull(html);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var id = htmlDoc.GetElementbyId("Id")?.InnerHtml?.Trim();
                Assert.IsFalse(string.IsNullOrEmpty(id));
                Assert.AreEqual(Guid.Parse(id), Guid.Parse("01abdad8-f460-4e03-96cb-8e130b95f3a1"));
            }

            await AspNetTestServer.Stop<Global>().ConfigureAwait(false);

            TestHelper.EnsureServerStopped();
        }
    }
}

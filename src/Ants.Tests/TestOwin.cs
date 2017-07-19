using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Ants.Web;
using NUnit.Framework;
using Owin;

namespace Ants.Tests
{
    [TestFixture]
    public class TestOwin
    {
        [Ignore("Experimental feature that is not ready."), Test]
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
                using (var request = new HttpRequestMessage(HttpMethod.Get, "/owin/test"))
                using (var result = await client.SendAsync(request).ConfigureAwait(false))
                {
                    Assert.IsNotNull(result);
                    Assert.IsNotNull(result.Content);

                    Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);

                    Debug.WriteLine(result);

                    foreach (var stage in Enum.GetValues(typeof(PipelineStage))
                        .Cast<int>()
                        .OrderBy(value => value)
                        .Cast<PipelineStage>())
                    {
                        //todo: map handler via owin is not currently supported over ANTS
                        if (stage == PipelineStage.MapHandler)
                        {
                            continue;
                        }

                        Assert.IsTrue(result.Headers.TryGetValues($"Owin-{stage}", out IEnumerable<string> values));
                        Assert.IsNotNull(values);

                        var valueStr = values.FirstOrDefault();
                        Assert.IsTrue(int.TryParse(valueStr, out int value));

                        Assert.AreEqual((int) stage, value);
                    }
                }
            }

            await AspNetTestServer.Stop<Global>().ConfigureAwait(false);

            TestHelper.EnsureServerStopped();
        }
    }
}

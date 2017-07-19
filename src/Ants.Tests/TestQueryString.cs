using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Ants.Web;
using Ants.Web.Constraints;
using Ants.Web.Filters;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Ants.Tests
{
    [TestFixture]
    public class TestQueryString
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
                var id = Guid.NewGuid();
                var queryString = Guid.NewGuid();

                using (var request = new HttpRequestMessage(HttpMethod.Get, $"/api/TestQueryString/{id}?{queryString}"))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    using (var result = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        Assert.IsNotNull(result);
                        Assert.IsNotNull(result.Content);

                        var constraintResults = result.Headers.GetValues(GuidConstraint.ConstraintResults).FirstOrDefault();
                        Assert.AreEqual(constraintResults, "true");
                        var filterResults = result.Headers.GetValues(IdFilter.FilterResults).FirstOrDefault();
                        Assert.AreEqual(filterResults, "true");

                        result.EnsureSuccessStatusCode();

                        Debug.WriteLine(result);

                        var json = await result.Content.ReadAsStringAsync();
                        Assert.IsFalse(string.IsNullOrEmpty(json));

                        var queryStringResults = JsonConvert.DeserializeObject<string>(json);
                        Assert.IsFalse(string.IsNullOrEmpty(queryStringResults));
                        Assert.IsTrue(queryStringResults.StartsWith("?"));
                        queryStringResults = queryStringResults.Substring(1);
                        Debug.WriteLine(queryStringResults);

                        Assert.AreEqual(queryStringResults, queryString.ToString());
                    }
                }
            }

            await AspNetTestServer.Stop<Global>().ConfigureAwait(false);

            TestHelper.EnsureServerStopped();
        }
    }
}

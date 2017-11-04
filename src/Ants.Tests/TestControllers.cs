using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Ants.Web;
using Ants.Web.Constraints;
using Ants.Web.Filters;
using Ants.Web.Models;
using HtmlAgilityPack;
using Newtonsoft.Json;
using NUnit.Framework;
using ParkSquare.Testing.Helpers;

namespace Ants.Tests
{
    [TestFixture]
    public class TestControllers
    {
        private static async Task testApi(HttpClient client, HttpMethod method, ApiRequest body = null)
        {
            var id = Guid.NewGuid();

            using (var request = new HttpRequestMessage(method, $"/api/TestApi/{id}"))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (body != null)
                {
                    request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                }

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
                    Debug.WriteLine(json);

                    var response = JsonConvert.DeserializeObject<ApiResponse>(json);
                    Assert.AreEqual(response.Id, id);
                    Assert.IsTrue(string.Equals(response.Method, method.Method, StringComparison.OrdinalIgnoreCase));

                    if (body != null)
                    {
                        Assert.IsNotNull(response.RequestId);
                        Assert.AreEqual(response.RequestId.Value, body.RequestId);
                    }
                }
            }
        }
        [Test]
        public async Task TestApiController()
        {
            TestHelper.EnsureNothingIsRunning();

            AspNetTestServer.Start<Global>(new StartApplicationArgs
            {
                PhysicalDirectory = TestHelper.WebProjectDirectory
            });

            TestHelper.EnsureServerStarted();

            using (var client = AspNetTestServer.GetHttpClient<Global>())
            {
                await testApi(client, HttpMethod.Delete).ConfigureAwait(false);
                await testApi(client, HttpMethod.Get).ConfigureAwait(false);
                await testApi(client, HttpMethod.Post, new ApiRequest()).ConfigureAwait(false);
                await testApi(client, HttpMethod.Put, new ApiRequest()).ConfigureAwait(false);
            }

            await AspNetTestServer.Stop<Global>().ConfigureAwait(false);

            TestHelper.EnsureServerStopped();
        }

        private static async Task testMvc(HttpClient client, HttpMethod method, MvcRequest body = null, bool multipartForm = true)
        {
            using (var request = new HttpRequestMessage(method, "/"))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

                if (body != null)
                {
                    if (multipartForm)
                    {
                        request.Content = new MultipartFormDataContent
                        {
                            { new StringContent(body.Id.ToString()), nameof(body.Id) },
                            { new StringContent(body.Name), nameof(body.Name) }
                        };
                    }
                    else
                    {
                        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            { nameof(body.Id), body.Id.ToString() },
                            { nameof(body.Name), body.Name }
                        });
                    }
                }

                using (var result = await client.SendAsync(request).ConfigureAwait(false))
                {
                    Assert.IsNotNull(result);
                    Assert.IsNotNull(result.Content);

                    result.EnsureSuccessStatusCode();

                    Debug.WriteLine(result);

                    var html = await result.Content.ReadAsStringAsync();
                    Assert.IsFalse(string.IsNullOrEmpty(html));
                    Debug.WriteLine(html);

                    if (body != null)
                    {
                        var htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(html);

                        var id = htmlDoc.GetElementbyId(nameof(body.Id))?.InnerHtml?.Trim();
                        Assert.IsFalse(string.IsNullOrEmpty(id));
                        Assert.AreEqual(Guid.Parse(id), body.Id);

                        var name = htmlDoc.GetElementbyId(nameof(body.Name))?.InnerHtml?.Trim();
                        Assert.IsFalse(string.IsNullOrEmpty(name));
                        Assert.AreEqual(name, body.Name);
                    }
                }
            }
        }
        [Test]
        public async Task TestMvcController()
        {
            TestHelper.EnsureNothingIsRunning();

            AspNetTestServer.Start<Global>(new StartApplicationArgs
            {
                PhysicalDirectory = TestHelper.WebProjectDirectory
            });

            TestHelper.EnsureServerStarted();

            using (var client = AspNetTestServer.GetHttpClient<Global>())
            {
                await testMvc(client, HttpMethod.Get).ConfigureAwait(false);
                await testMvc(client, HttpMethod.Post, new MvcRequest
                {
                    Name = NameGenerator.AnyName()
                }).ConfigureAwait(false);
                await testMvc(client, HttpMethod.Post, new MvcRequest
                {
                    Name = NameGenerator.AnyName()
                },
                multipartForm: false).ConfigureAwait(false);
            }

            await AspNetTestServer.Stop<Global>().ConfigureAwait(false);

            TestHelper.EnsureServerStopped();
        }
    }
}

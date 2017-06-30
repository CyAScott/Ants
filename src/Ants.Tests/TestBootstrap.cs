using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Ants.Web;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Ants.Tests
{
    [TestFixture]
    public class TestBootstrap
    {
        public class TestAppDomainWorker : AppDomainWorker
        {
            public const string AnotherConfigValue = "9e94cabe-bd0d-45a3-aab4-78a424004e65";
            public const string TestConfigValue = "1e0e347c-409b-449f-a2dc-58563a17a49d";

            public override void BeforeApplicationStarts()
            {
                Global.AppSettings.Set("SomeConfigValue", TestConfigValue);
            }
        }
        [Test]
        public async Task AppDomainWorker()
        {
            TestHelper.EnsureNothingIsRunning();

            AspNetTestServer.Start<Global>(new StartApplicationArgs<TestAppDomainWorker>
            {
                PhysicalDirectory = TestHelper.WebProjectDirectory
            });

            TestHelper.EnsureServerStarted();

            using (var client = AspNetTestServer.GetHttpClient<Global>())
            using (var request = new HttpRequestMessage(HttpMethod.Get, "api/Configs/AppSettings"))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var result = await client.SendAsync(request).ConfigureAwait(false))
                {
                    Assert.IsNotNull(result);
                    Assert.IsNotNull(result.Content);

                    result.EnsureSuccessStatusCode();

                    Debug.WriteLine(result);

                    var json = await result.Content.ReadAsStringAsync();
                    Assert.IsFalse(string.IsNullOrEmpty(json));
                    Debug.WriteLine(json);

                    var appSettings = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                    Assert.IsTrue(appSettings.TryGetValue(nameof(TestAppDomainWorker.AnotherConfigValue), out string anotherConfigValue));
                    Assert.AreEqual(Guid.Parse(TestAppDomainWorker.AnotherConfigValue), Guid.Parse(anotherConfigValue));

                    Assert.IsTrue(appSettings.TryGetValue("SomeConfigValue", out string testConfigValue));
                    Assert.AreEqual(Guid.Parse(TestAppDomainWorker.TestConfigValue), Guid.Parse(testConfigValue));
                }
            }

            await AspNetTestServer.Stop<Global>().ConfigureAwait(false);

            TestHelper.EnsureServerStopped();
        }

        public class DefaultDomainWorker : MarshalByRefObject
        {
            public static TaskCompletionSource<bool> AfterApplicationStartsCalled { get; set; } = new TaskCompletionSource<bool>();
            public void AfterApplicationStartsCallback()
            {
                AfterApplicationStartsCalled.TrySetResult(true);
            }

            public static TaskCompletionSource<bool> BeforeApplicationStartsCalled { get; } = new TaskCompletionSource<bool>();
            public void BeforeApplicationStartsCallback()
            {
                BeforeApplicationStartsCalled.TrySetResult(true);
            }
        }
        public class TestAppDomainWorkerWithDefaultWorker : AppDomainWorker<DefaultDomainWorker>
        {
            public override void AfterApplicationStarts()
            {
                DefaultAppDomainWorker.AfterApplicationStartsCallback();
            }

            public override void BeforeApplicationStarts()
            {
                DefaultAppDomainWorker.BeforeApplicationStartsCallback();
            }
        }
        [Test]
        public async Task AppDomainWorkerWithDefaultWorker()
        {
            TestHelper.EnsureNothingIsRunning();

            Assert.IsFalse(DefaultDomainWorker.AfterApplicationStartsCalled.Task.IsCompleted);
            Assert.IsFalse(DefaultDomainWorker.BeforeApplicationStartsCalled.Task.IsCompleted);

            AspNetTestServer.Start<Global>(new StartApplicationArgs<TestAppDomainWorkerWithDefaultWorker>
            {
                PhysicalDirectory = TestHelper.WebProjectDirectory
            });

            TestHelper.EnsureServerStarted();

            await Task.WhenAny(Task.Delay(TimeSpan.FromMinutes(5)), Task.WhenAll(
                DefaultDomainWorker.AfterApplicationStartsCalled.Task, 
                DefaultDomainWorker.BeforeApplicationStartsCalled.Task))
                .ConfigureAwait(false);

            Assert.IsTrue(DefaultDomainWorker.AfterApplicationStartsCalled.Task.IsCompleted);
            Assert.IsTrue(DefaultDomainWorker.BeforeApplicationStartsCalled.Task.IsCompleted);

            await AspNetTestServer.Stop<Global>().ConfigureAwait(false);

            TestHelper.EnsureServerStopped();
        }
    }
}

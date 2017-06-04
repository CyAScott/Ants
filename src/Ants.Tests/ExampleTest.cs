using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Ants.Web;
using NUnit.Framework;

namespace Ants.Tests
{
    [TestFixture]
    public class ExampleTest
    {
        /// <summary>
        /// This class is created once and exists in the default domain.
        /// It is used to process work on the default domain.
        /// </summary>
        public class DefaultAppDomainWorker : MarshalByRefObject
        {
            public void ApplicationStarted(AppDomainWorker worker)
            {
                Debug.WriteLine("Another ASP.NET application started.");
            }
        }

        /// <summary>
        /// This class is created when the ASP.NET app domain is created.
        /// It is used to process work on the ASP.NET's app domain.
        /// </summary>
        public class AppDomainWorker : AppDomainWorker<DefaultAppDomainWorker>
        {
            public override void AfterApplicationStarts()
            {
                //here you can run code after the ASP.NET application starts

                //here is a proxy call to the default domain to process work on the default domain
                DefaultAppDomainWorker.ApplicationStarted(this);
            }

            public override void BeforeApplicationStarts()
            {
                //here is where you can set config settings for the test (i.e. connection strings, etc.)

                //you can create an http client that can make requests to other ASP.NET applications running in ANTS
                //using (var client = AspNetTestServer.GetHttpClient<SomeOtherAspNetApplication>())
                //{
                //}
            }
        }

        [Ignore("Test example."), Test]
        public async Task Test()
        {
            AspNetTestServer.IsDefaultAppDomain = true;

            AspNetTestServer.Start<Global>(new StartApplicationArgs<AppDomainWorker>
            {
                PhysicalDirectory = TestHelper.WebProjectDirectory
            });

            // ReSharper disable once UnusedVariable
            using (var client = AspNetTestServer.GetHttpClient<Global>())
            {
                //here we can make rest calls to the ASP.NET application
            }

            await AspNetTestServer.Stop<Global>().ConfigureAwait(false);
        }
    }
}

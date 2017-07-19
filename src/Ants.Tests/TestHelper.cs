using System;
using System.IO;
#if DEBUG
using NUnit.Framework;
#endif

namespace Ants.Tests
{
    public static class TestHelper
    {
        private static readonly Lazy<string> webProjectDirectory = new Lazy<string>(() =>
        {
            var returnValue = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

            while (!string.IsNullOrEmpty(returnValue) && !Directory.Exists(Path.Combine(returnValue, "Ants.Web")))
            {
                returnValue = Path.GetDirectoryName(returnValue);
            }

            return Path.Combine(returnValue ?? "", "Ants.Web");
        });
        public static string WebProjectDirectory => webProjectDirectory.Value;

        public static void EnsureNothingIsRunning()
        {
            AspNetTestServer.IsDefaultAppDomain = true;
            AspNetTestServer.HttpMessageHandler.ClearCookies();

#if DEBUG
            Assert.IsFalse(Testing.Variables.ApplicationAuthenticateRequestCalled);
            Assert.IsFalse(Testing.Variables.ApplicationBeginRequestCalled);
            Assert.IsFalse(Testing.Variables.ApplicationEndCalled);
            Assert.IsFalse(Testing.Variables.ApplicationErrorCalled);
            Assert.IsFalse(Testing.Variables.ApplicationStartCalled);
            Assert.IsFalse(Testing.Variables.ApplicationStartCompleted);
            Assert.IsFalse(Testing.Variables.SessionEndCalled);
            Assert.IsFalse(Testing.Variables.SessionStartCalled);
#endif
        }
        public static void EnsureServerStarted()
        {
#if DEBUG
            Assert.IsTrue(Testing.Variables.ApplicationBeginRequestCalled);
            Assert.IsTrue(Testing.Variables.ApplicationStartCalled);
            Assert.IsTrue(Testing.Variables.ApplicationStartCompleted);
            Assert.IsTrue(Testing.Variables.SessionStartCalled);
#endif
        }
        public static void EnsureServerStopped()
        {
#if DEBUG
            Assert.IsTrue(Testing.Variables.ApplicationEndCalled);

            Testing.Variables.ApplicationAuthenticateRequestCalled = false;
            Testing.Variables.ApplicationBeginRequestCalled = false;
            Testing.Variables.ApplicationEndCalled = false;
            Testing.Variables.ApplicationErrorCalled = false;
            Testing.Variables.ApplicationStartCalled = false;
            Testing.Variables.ApplicationStartCompleted = false;
            Testing.Variables.SessionEndCalled = false;
            Testing.Variables.SessionStartCalled = false;
#endif
        }
    }
}

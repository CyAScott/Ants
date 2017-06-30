using System;
using System.Reflection;
using System.Threading;
using System.Web;

// ReSharper disable StaticMemberInGenericType

namespace Ants.Owin
{
    internal class OwinHttpModuleWrapper<THttpModule> : IHttpModule
        where THttpModule : IHttpModule, new()
    {
        private THttpModule owinModule;
        private class AsyncResult : IAsyncResult, IDisposable
        {
            public WaitHandle AsyncWaitHandle { get; private set; } = new ManualResetEvent(false);
            public bool IsCompleted { get; } = true;
            public bool CompletedSynchronously { get; } = true;
            public object AsyncState { get; } = null;
            public void Dispose()
            {
                var handle = AsyncWaitHandle;
                AsyncWaitHandle = null;
                handle?.Dispose();
            }
        }
        private static IAsyncResult OnEventForRequestStart(object sender, EventArgs eventArgs, AsyncCallback cb, object extraData)
        {
            lock (sender)
            {
                var application = (HttpApplication)sender;
                var context = application.Context.Request.RequestContext as AntsRequestContext;
                if (context == null)
                {
                    application.Context.Request.RequestContext = new AntsRequestContext(application.Context);
                }
            }
            return new AsyncResult();
        }
        private static int eventsRegistered;
        private static void OnEventForRequestStop(IAsyncResult ar)
        {
            var results = ar as IDisposable;
            results?.Dispose();
        }

        public void Init(HttpApplication application)
        {
            owinModule = new THttpModule();

            application.Context.Request.RequestContext = new AntsRequestContext(application.Context);

            if (Interlocked.CompareExchange(ref eventsRegistered, 1, 0) == 0)
            {
                //typeof(HttpRuntime)
                //    .GetField("_useIntegratedPipeline", BindingFlags.NonPublic | BindingFlags.Static)
                //    ?.SetValue(null, true);
                application.AddOnAuthenticateRequestAsync(OnEventForRequestStart, OnEventForRequestStop);
                application.AddOnPostAuthenticateRequestAsync(OnEventForRequestStart, OnEventForRequestStop);
                application.AddOnAuthorizeRequestAsync(OnEventForRequestStart, OnEventForRequestStop);
                application.AddOnPostAuthorizeRequestAsync(OnEventForRequestStart, OnEventForRequestStop);
                application.AddOnResolveRequestCacheAsync(OnEventForRequestStart, OnEventForRequestStop);
                application.AddOnPostResolveRequestCacheAsync(OnEventForRequestStart, OnEventForRequestStop);
                //application.AddOnMapRequestHandlerAsync(OnEventForRequestStart, OnEventForRequestStop);
                application.AddOnPostMapRequestHandlerAsync(OnEventForRequestStart, OnEventForRequestStop);
                application.AddOnAcquireRequestStateAsync(OnEventForRequestStart, OnEventForRequestStop);
                application.AddOnPostAcquireRequestStateAsync(OnEventForRequestStart, OnEventForRequestStop);
                application.AddOnPreRequestHandlerExecuteAsync(OnEventForRequestStart, OnEventForRequestStop);
            }

            owinModule.Init(application);
        }

        public void Dispose()
        {
            owinModule.Dispose();
        }
    }
}

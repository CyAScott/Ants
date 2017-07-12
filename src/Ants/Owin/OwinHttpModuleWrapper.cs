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
        private static IAsyncResult OnEventForRequestStart(object sender, EventArgs eventArgs, AsyncCallback cb, object extraData)
        {
            lock (locker)
            {
                var application = (HttpApplication)sender;
                var context = application.Context.Request.RequestContext as AntsRequestContext;
                if (context == null)
                {
                    application.Context.Request.RequestContext = new AntsRequestContext(application.Context);
                }

                var beginEventDelegate = (BeginEventHandler)extraData;

                return beginEventDelegate(sender, eventArgs, cb, null);
            }
        }

        private static int initializeStarted;
        private static object blueprint, integratedPipelineContext;
        private static readonly object locker = new object();

        public void Init(HttpApplication application)
        {
            lock (locker)
            {
                application.Context.Request.RequestContext = new AntsRequestContext(application.Context);

                if (Interlocked.CompareExchange(ref initializeStarted, 1, 0) == 0)
                {
                    //todo: fix issue with processing requests with this value set to true
                    //typeof(HttpRuntime)
                    //    .GetField("_useIntegratedPipeline", BindingFlags.NonPublic | BindingFlags.Static)
                    //    ?.SetValue(null, true);

                    owinModule = new THttpModule();

                    blueprint = typeof(THttpModule)
                        .GetMethod("InitializeBlueprint", BindingFlags.NonPublic | BindingFlags.Instance)
                        .Invoke(owinModule, null);

                    integratedPipelineContext = typeof(THttpModule)
                        .Assembly
                        .GetType("Microsoft.Owin.Host.SystemWeb.IntegratedPipeline.IntegratedPipelineContext")
                        .GetConstructor(new[] { blueprint.GetType() })
                        ?.Invoke(new[] { blueprint });
                }

                Initialize(application);
            }
        }

        public static void Initialize(HttpApplication application)
        {
            var integratedPipelineContextType = integratedPipelineContext.GetType();
            var beginFinalWork = (BeginEventHandler)integratedPipelineContextType
                .GetMethod("BeginFinalWork", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.CreateDelegate(typeof(BeginEventHandler), integratedPipelineContext);
            var endFinalWork = (EndEventHandler)integratedPipelineContextType
                .GetMethod("EndFinalWork", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.CreateDelegate(typeof(EndEventHandler), integratedPipelineContext);
            if (beginFinalWork == null || endFinalWork == null)
            {
                throw new InvalidOperationException("Unable to find the BeginFinalWork and EndFinalWork methods.");
            }

            var integratedPipelineContextStageType = typeof(THttpModule)
                .Assembly
                .GetType("Microsoft.Owin.Host.SystemWeb.IntegratedPipeline.IntegratedPipelineContextStage");
            var ctor = integratedPipelineContextStageType
                ?.GetConstructor(new[] { integratedPipelineContextType, typeof(THttpModule)
                    .Assembly
                    .GetType("Microsoft.Owin.Host.SystemWeb.IntegratedPipeline.IntegratedPipelineBlueprintStage") });
            if (ctor == null)
            {
                throw new InvalidOperationException("Unable to find the ctor for Microsoft.Owin.Host.SystemWeb.IntegratedPipeline.IntegratedPipelineContextStage.");
            }

            var beginEvent = integratedPipelineContextStageType.GetMethod("BeginEvent");
            var endEvent = integratedPipelineContextStageType.GetMethod("EndEvent");
            if (beginEvent == null || endEvent == null)
            {
                throw new InvalidOperationException("Unable to find the BeginEvent and EndEvent methods.");
            }

            var blueprintType = blueprint.GetType();

            var firstStage = blueprintType.GetProperty("FirstStage");

            var name = firstStage?.PropertyType.GetProperty("Name");
            var nextStage = firstStage?.PropertyType.GetProperty("NextStage");

            if (firstStage == null || name == null || nextStage == null)
            {
                throw new InvalidOperationException("Unable to find the FirstStage and NextStage properties.");
            }

            for (var stage = firstStage.GetValue(blueprint); stage != null; stage = nextStage.GetValue(stage))
            {
                var segment = ctor.Invoke(new[] { integratedPipelineContext, stage });
                var beginEventDelegate = (BeginEventHandler)beginEvent.CreateDelegate(typeof(BeginEventHandler), segment);
                var endEventDelegate = (EndEventHandler)endEvent.CreateDelegate(typeof(EndEventHandler), segment);
                switch ((string)name.GetValue(stage))
                {
                    case "Authenticate":
                        application.AddOnAuthenticateRequestAsync(OnEventForRequestStart, endEventDelegate, beginEventDelegate);
                        break;
                    case "PostAuthenticate":
                        application.AddOnPostAuthenticateRequestAsync(OnEventForRequestStart, endEventDelegate, beginEventDelegate);
                        break;
                    case "Authorize":
                        application.AddOnAuthorizeRequestAsync(OnEventForRequestStart, endEventDelegate, beginEventDelegate);
                        break;
                    case "PostAuthorize":
                        application.AddOnPostAuthorizeRequestAsync(OnEventForRequestStart, endEventDelegate, beginEventDelegate);
                        break;
                    case "ResolveCache":
                        application.AddOnResolveRequestCacheAsync(OnEventForRequestStart, endEventDelegate, beginEventDelegate);
                        break;
                    case "PostResolveCache":
                        application.AddOnPostResolveRequestCacheAsync(OnEventForRequestStart, endEventDelegate, beginEventDelegate);
                        break;
                    //todo: _useIntegratedPipeline needs to be set to true for this to work
                    //case "MapHandler":
                    //    application.AddOnMapRequestHandlerAsync(OnEventForRequestStart, endEventDelegate, beginEventDelegate);
                    //    break;
                    case "PostMapHandler":
                        application.AddOnPostMapRequestHandlerAsync(OnEventForRequestStart, endEventDelegate, beginEventDelegate);
                        break;
                    case "AcquireState":
                        application.AddOnAcquireRequestStateAsync(OnEventForRequestStart, endEventDelegate, beginEventDelegate);
                        break;
                    case "PostAcquireState":
                        application.AddOnPostAcquireRequestStateAsync(OnEventForRequestStart, endEventDelegate, beginEventDelegate);
                        break;
                    case "PreHandlerExecute":
                        application.AddOnPreRequestHandlerExecuteAsync(OnEventForRequestStart, endEventDelegate, beginEventDelegate);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

            application.AddOnEndRequestAsync(OnEventForRequestStart, endFinalWork, beginFinalWork);
        }

        public void Dispose()
        {
            owinModule?.Dispose();
        }
    }
}

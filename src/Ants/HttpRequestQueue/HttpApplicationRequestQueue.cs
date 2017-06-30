using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using Ants.AutoLoader;

namespace Ants.HttpRequestQueue
{
    internal class HttpApplicationRequestQueue : MarshalByRefObject, IRegisteredObject
    {
        private bool enabled = true;
        private int firstRequest = 1, threadCount;
        private readonly ConcurrentQueue<Message> requests = new ConcurrentQueue<Message>();
        private void afterFirstRequest()
        {
            if (Interlocked.CompareExchange(ref firstRequest, 0, 1) == 0)
            {
                return;
            }

            //execute auto loaded assembly code before the application starts
            foreach (var helper in Helpers)
            {
                helper.AfterFirstRouteLoaded();
            }
            StartApplicationArgs.InvokeAfterApplicationStartsOnDomainWorker();
        }
        private void processRequest()
        {
            if (!requests.TryDequeue(out Message request))
            {
                return;
            }

            try
            {
                var workerRequest = new HttpWorkerRequestMessage(request);
                HttpContext.Current = new HttpContext(workerRequest);
                HttpRuntime.ProcessRequest(workerRequest);
                afterFirstRequest();
            }
            catch (Exception error)
            {
                request.EndOfRequestWithError(error.Message, error.StackTrace, error.GetType().AssemblyQualifiedName);
            }
        }
        private void processRequests(object state)
        {
            //process all requests in the queue
            while (!requests.IsEmpty && enabled)
            {
                processRequest();
            }

            //decrement the active thread count
            Interlocked.Decrement(ref threadCount);

            //process any requests that were added to the queue after the first loop exited but before the decrement was executed
            while (threadCount == 0 && !requests.IsEmpty && enabled)
            {
                processRequest();
            }
        }

#if DEBUG
        public void SetTestingVariables(Testing variables)
        {
            Testing.Variables = variables;
        }
#endif

        public ApplicationManager ApplicationManager { get; set; }
        public AutoLoadAssemblyHelper[] Helpers { get; set; }
        public DefaultDomainWorker DefaultDomainWorker { get; set; }
        public StartApplicationArgs StartApplicationArgs { get; set; }
        public Type AppType { get; set; }
        public int MaxThreads { get; set; }
        public string Domain { get; set; }
        public void Enqueue(Message request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!enabled)
            {
                throw new InvalidOperationException("This ASP.NET application is shutting down.");
            }

            requests.Enqueue(request);

            if (Interlocked.Increment(ref threadCount) <= MaxThreads)
            {
                ThreadPool.QueueUserWorkItem(processRequests);
            }
            else
            {
                Interlocked.Decrement(ref threadCount);
            }
        }
        public void Init()
        {
            AppDomain.CurrentDomain.DomainUnload += (sender, args) => DefaultDomainWorker.DomainClosed(Domain);

            AspNetTestServer.ApplicationManager = ApplicationManager;
            AspNetTestServer.DefaultDomainWorker = DefaultDomainWorker;

            HttpModuleHelper.ReplaceOwinHttpModule();
        }
        public void Stop(bool immediate)
        {
            enabled = false;
            while (!requests.IsEmpty)
            {
                if (requests.TryDequeue(out Message request))
                {
                    request?.EndOfRequestWithShutDown();
                }
            }
        }
    }
}

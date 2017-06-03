using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Hosting;

namespace Ants
{
    internal class HttpApplicationRequestQueue : MarshalByRefObject, IRegisteredObject
    {
        private bool enabled = true;
        private int threadCount;
        private readonly ConcurrentQueue<Message> requests = new ConcurrentQueue<Message>();
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
            }
            catch (Exception error)
            {
                request.EndOfRequestWithError(error.Message, error.StackTrace, error.GetType().FullName);
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

        public DefaultDomainWorker Worker { get; set; }
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
        public void Init(string firstRouteToLoad)
        {
            AppDomain.CurrentDomain.DomainUnload += (sender, args) => 
            Worker.DomainClosed(Domain);

            using (var stream = new MemoryStream())
            using (var output = new StreamWriter(stream))
            {
                var request = new SimpleWorkerRequest(firstRouteToLoad, null, output);
                HttpContext.Current = new HttpContext(request);
                HttpRuntime.ProcessRequest(request);
#if DEBUG
                output.Flush();
                stream.Position = 0;
                var response = Encoding.UTF8.GetString(stream.ToArray());

                Debug.WriteLine(response);
#endif
            }
        }
        public void Stop(bool immediate)
        {
            enabled = false;
            while (!requests.IsEmpty)
            {
                Message request;
                if (requests.TryDequeue(out request))
                {
                    request?.EndOfRequestWithShutDown();
                }
            }
        }
    }
}

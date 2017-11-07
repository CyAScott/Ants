using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ants.HttpRequestQueue
{
    internal class DefaultDomainWorker : MarshalByRefObject
    {
        public EnqueueResults Enqueue(string domain, Message message)
        {
            if (!AspNetTestServer.Applications.TryGetValue(domain, out HttpApplicationRequestQueue applicationRequestQueue))
            {
                return EnqueueResults.ApplicationNotFound;
            }

            if (applicationRequestQueue == null)
            {
                return EnqueueResults.ApplicationNotInitialize;
            }

            applicationRequestQueue.Enqueue(message);

            return EnqueueResults.Enqueued;
        }
        public string GetDomainFromId(Guid id)
        {
            return AspNetTestServer.Applications.Values.FirstOrDefault(queue => queue.Id == id)?.Domain;
        }
        public void DomainClosed(string domain)
        {
            // ReSharper disable once UnusedVariable
            AspNetTestServer.Applications.TryRemove(domain, out HttpApplicationRequestQueue queue);
            if (AspNetTestServer.CloseTasks.TryRemove(domain, out TaskCompletionSource<object> onCloseTask))
            {
                onCloseTask.TrySetResult(null);
            }
        }
    }
    internal enum EnqueueResults
    {
        ApplicationNotFound,
        ApplicationNotInitialize,
        Enqueued
    }
}

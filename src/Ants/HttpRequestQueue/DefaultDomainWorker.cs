﻿using System;
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
        public string GetDomainFromType(string typeFullName)
        {
            return AspNetTestServer.Applications.Values.FirstOrDefault(queue => queue.AppType.AssemblyQualifiedName == typeFullName)?.Domain;
        }
        public void DomainClosed(string domain)
        {
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
using System;
using System.Threading.Tasks;

namespace Ants
{
    internal class DefaultDomainWorker : MarshalByRefObject
    {
        public void DomainClosed(string domain)
        {
            if (AspNetTestServer.CloseTasks.TryRemove(domain, out TaskCompletionSource<object> onCloseTask))
            {
                onCloseTask.TrySetResult(null);
            }
        }
    }
}

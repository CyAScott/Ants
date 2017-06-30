using System;
using System.Web.Hosting;
using Ants.HttpRequestQueue;

namespace Ants
{
    /// <summary>
    /// A class that is created on the same <see cref="AppDomain"/> as a target
    /// ASP.NET application. It should be used to do work on the
    /// <see cref="AppDomain"/> for the ASP.NET application.
    /// </summary>
    public abstract class AppDomainWorker : MarshalByRefObject, IRegisteredObject
    {
        internal DefaultDomainWorker DefaultDomainWorker { get; set; }
        internal virtual Type DefaultAppDomainWorkerType => null;
        internal virtual void SetDefaultAppDomainWorker(object defaultAppDomainWorker)
        {
        }

        /// <summary>
        /// The arguments for starting the ASP.NET application.
        /// </summary>
        public StartApplicationArgs StartApplicationArgs { get; internal set; }

        /// <summary>
        /// Is called when after the ASP.NET application starts,
        /// unless the LoadFirstRoute value in the start arguments was set to false.
        /// </summary>
        public virtual void AfterApplicationStarts()
        {
        }

        /// <summary>
        /// Is called before the ASP.NET application starts.
        /// </summary>
        public virtual void BeforeApplicationStarts()
        {
        }

        void IRegisteredObject.Stop(bool immediate)
        {
        }
    }

    /// <summary>
    /// A class that is created on the same <see cref="AppDomain"/> as a target
    /// ASP.NET application. It should be used to do work on the
    /// <see cref="AppDomain"/> for the ASP.NET application.
    /// </summary>
    /// <typeparam name="TDefaultDomainWorker">
    /// The default <see cref="AppDomain"/> worker that is used by the this
    /// <see cref="AppDomain"/> worker to communicate with the default domain
    /// <see cref="AppDomain"/>.
    /// </typeparam>
    public abstract class AppDomainWorker<TDefaultDomainWorker> : AppDomainWorker
        where TDefaultDomainWorker : MarshalByRefObject, new()
    {
        internal override Type DefaultAppDomainWorkerType => typeof(TDefaultDomainWorker);
        internal override void SetDefaultAppDomainWorker(object defaultAppDomainWorker)
        {
            DefaultAppDomainWorker = (TDefaultDomainWorker)defaultAppDomainWorker;
        }

        /// <summary>
        /// The default <see cref="AppDomain"/> worker that is used by the this
        /// <see cref="AppDomain"/> worker to communicate with the default domain
        /// <see cref="AppDomain"/>. By design this instance will be a singleton
        /// across all ASP.NET application instances.
        /// </summary>
        public TDefaultDomainWorker DefaultAppDomainWorker { get; private set; }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Hosting;

namespace Ants
{
    /// <summary>
    /// The arguments for starting an ASP.NET application.
    /// </summary>
    public class StartApplicationArgs : MarshalByRefObject
    {
        internal static readonly ConcurrentDictionary<Type, object> DefaultAppDomainWorkers = new ConcurrentDictionary<Type, object>();
        internal virtual void InvokeAfterApplicationStartsOnDomainWorker()
        {
        }
        internal void Sanitize(Guid id)
        {
            if (string.IsNullOrEmpty(Domain))
            {
                Domain = id.ToString();
            }
        }
        internal void ThrowOnValidationError()
        {
            if (ThreadCount <= 0)
            {
                throw new IndexOutOfRangeException(nameof(ThreadCount));
            }

            if (string.IsNullOrEmpty(PhysicalDirectory) || !Directory.Exists(PhysicalDirectory))
            {
                throw new DirectoryNotFoundException(PhysicalDirectory ?? "");
            }

            if (string.IsNullOrEmpty(VirtualDirectory))
            {
                throw new ArgumentException(nameof(VirtualDirectory));
            }

            if (string.IsNullOrEmpty(Domain) || Uri.CheckHostName(Regex.Replace(Domain, @":\d+$", "")) == UriHostNameType.Unknown)
            {
                throw new ArgumentException(nameof(Domain));
            }
        }

        /// <summary>
        /// When overridden by a derived class, this method will be invoked on the default <see cref="AppDomain"/> and to create
        /// a domain worker.
        /// </summary>
        /// <param name="buildManagerHost">Provides a set of methods to help manage the compilation of an ASP.NET application.</param>
        /// <param name="appDomain">The <see cref="AppDomain"/> the ASP.NET application will run on.</param>
        protected internal virtual void BootstrapDomainWorker(IRegisteredObject buildManagerHost, AppDomain appDomain)
        {
            //when the start arguments include a domain worker, this method will be overridden to create that domain work
        }

        /// <summary>
        /// Optional. Code that is invoked after the assemblies are loaded into the <see cref="AppDomain"/> but before the first route is loaded.
        /// This code runs in the default domain.
        /// The argument is a reference to the <see cref="AppDomain"/> the ASP.NET application will run in.
        /// </summary>
        [Obsolete("Use the AppDomainWorker instead.")]
        public Action<AppDomain> PreInitialize { get; set; }

        /// <summary>
        /// Optional. Assemblies to load into the <see cref="AppDomain"/> that houses the ASP.NET application.
        /// </summary>
        public IEnumerable<Assembly> AssembliesToLoad { get; set; }

        /// <summary>
        /// The max number of threads for processing requests.
        /// </summary>
        public int ThreadCount { get; set; } = 1;

        /// <summary>
        /// The domain you want to use for the simulation.
        /// If no value is provided then the application GUID is used.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// The first route to load to start the ASP.NET application.
        /// The default value is "favicon.ico".
        /// If the value is null then no route will be loaded to start the http application.
        /// </summary>
        public string FirstRouteToLoad { get; set; } = "favicon.ico";

        /// <summary>
        /// The project directory where the ASP.NET application exists.
        /// This folder is used by the simulated server just like IIS or IIS express does for hosting the site.
        /// </summary>
        public string PhysicalDirectory { get; set; }

        /// <summary>
        /// The virtual directory is the path in the url proceeding all the possible file paths in the physical directory.
        /// The default value is "/".
        /// </summary>
        public string VirtualDirectory { get; set; } = "/";
    }

    /// <summary>
    /// The arguments for starting an ASP.NET application.
    /// </summary>
    /// <typeparam name="TAppDomainWorker">A worker that will be created on the ASP.NET application <see cref="AppDomain"/>.</typeparam>
    public class StartApplicationArgs<TAppDomainWorker> : StartApplicationArgs
        where TAppDomainWorker : AppDomainWorker, new()
    {
        internal override void InvokeAfterApplicationStartsOnDomainWorker()
        {
            appDomainWorker.AfterApplicationStarts();
        }
        private TAppDomainWorker appDomainWorker;

        /// <summary>
        /// Creates a TAppDomainWorker on the same <see cref="AppDomain"/> as the target ASP.NET application.
        /// </summary>
        /// <param name="buildManagerHost">Provides a set of methods to help manage the compilation of an ASP.NET application.</param>
        /// <param name="appDomain">The <see cref="AppDomain"/> the ASP.NET application will run on.</param>
        /// <exception cref="ApplicationException">Failed to load the App Domain worker.</exception>
        protected internal override void BootstrapDomainWorker(IRegisteredObject buildManagerHost, AppDomain appDomain)
        {
            //register the assembly for the domain worker
            buildManagerHost.RegisterAssembly(appDomain, typeof(TAppDomainWorker).Assembly);

            var applicationManager = AspNetTestServer.ApplicationManager;

            //create the domain worker on the target app domain
            appDomainWorker = applicationManager.CreateObject(Domain, typeof(TAppDomainWorker), VirtualDirectory, PhysicalDirectory, false) as TAppDomainWorker;
            if (appDomainWorker == null)
            {
                throw new ApplicationException("Failed to load the App Domain worker.");
            }
            appDomainWorker.StartApplicationArgs = this;

            //connect the ANTS domain worker (used for access to other ASP.NET applications across domains)
            appDomainWorker.DefaultDomainWorker = AspNetTestServer.DefaultDomainWorker;

            //if the domain worker includes a default domain worker property then
            var defaultAppDomainWorkerType = appDomainWorker.DefaultAppDomainWorkerType;
            if (defaultAppDomainWorkerType != null)
            {
                //create or get singleton instance of that default domain worker
                if (!DefaultAppDomainWorkers.TryGetValue(defaultAppDomainWorkerType, out object worker))
                {
                    DefaultAppDomainWorkers[defaultAppDomainWorkerType] = worker = Activator.CreateInstance(defaultAppDomainWorkerType);
                }
                appDomainWorker.SetDefaultAppDomainWorker(worker);
            }

            //bootstrap the test configurations
            appDomainWorker.BeforeApplicationStarts();
        }
    }
}

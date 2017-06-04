using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace Ants
{
    /// <summary>
    /// A static class for managing ASP.NET applications running on a simulated server.
    /// </summary>
    public static class AspNetTestServer
    {
        internal static DefaultDomainWorker DefaultDomainWorker { get; set; }
        internal static HttpApplicationRequestQueue GetApplication<THttpApplication>()
            where THttpApplication : HttpApplication, new()
        {
            return Applications.Values.FirstOrDefault(queue => queue.AppType == typeof(THttpApplication));
        }
        internal static readonly ConcurrentDictionary<string, HttpApplicationRequestQueue> Applications = new ConcurrentDictionary<string, HttpApplicationRequestQueue>(StringComparer.OrdinalIgnoreCase);
        internal static readonly ConcurrentDictionary<string, TaskCompletionSource<object>> CloseTasks = new ConcurrentDictionary<string, TaskCompletionSource<object>>(StringComparer.OrdinalIgnoreCase);

        private static void throwIfNotDefaultAppDomain()
        {
            if (!IsDefaultAppDomain)
            {
                throw new InvalidOperationException("The test server can only be accessed from the default domain.");
            }

            ApplicationManager = ApplicationManager ?? ApplicationManager.GetApplicationManager();
            DefaultDomainWorker = DefaultDomainWorker ?? new DefaultDomainWorker();
        }

        /// <summary>
        /// Starts hosting an ASP.NET application in a simulated server.
        /// </summary>
        /// <exception cref="ApplicationException">Failed to load the App Domain worker.</exception>
        /// <exception cref="ApplicationException">Failed to load the request queue for processing HTTP requests.</exception>
        /// <exception cref="ArgumentException">Virtual Directory</exception>
        /// <exception cref="ArgumentException">Domain</exception>
        /// <exception cref="DirectoryNotFoundException">Physical Directory</exception>
        /// <exception cref="IndexOutOfRangeException">Thread Count</exception>
        /// <exception cref="InvalidOperationException">The test server can only be accessed from the default domain.</exception>
        /// <exception cref="InvalidOperationException">An ASP.NET application is already running.</exception>
        /// <typeparam name="THttpApplication">The type for the ASP.NET application.</typeparam>
        /// <param name="args">Optional. The arguments for starting an ASP.NET application.</param>
        /// <returns>The <see cref="AppDomain"/> the ASP.NET application runs in.</returns>
        public static AppDomain Start<THttpApplication>(StartApplicationArgs args = null)
            where THttpApplication : HttpApplication, new()
        {
            throwIfNotDefaultAppDomain();

            args = args ?? new StartApplicationArgs();

            AppDomain returnValue;

            lock (args)
            {
#if DEBUG
                //test variables used to test the ANTS framework
                var testingVariables = Testing.Variables;
#endif

                args.Sanitize<THttpApplication>();

                args.ThrowOnValidationError();

                //by adding a null value this makes setting up apps thread safe
                if (!Applications.TryAdd(args.Domain, null))
                {
                    throw new InvalidOperationException("An ASP.NET application is already running.");
                }

                //create the app domain for the ASP.NET application to run on
                var buildManagerHost = ApplicationManager.CreateObject(args.Domain, Extensions.BuildManagerHostType, args.VirtualDirectory, args.PhysicalDirectory, true);

                //register the assembly for ANTS for the target app domain
                var applicationRequestQueueType = typeof(HttpApplicationRequestQueue);
                buildManagerHost.RegisterAssembly(applicationRequestQueueType.Assembly);

                //register any assemblies provided by the developer
                if (args.AssembliesToLoad != null)
                {
                    foreach (var assembly in args.AssembliesToLoad)
                    {
                        buildManagerHost.RegisterAssembly(assembly);
                    }
                }

                //create the ASP.NET application message queue for processing http requests
                var applicationRequestQueue = ApplicationManager.CreateObject(args.Domain, applicationRequestQueueType, args.VirtualDirectory, args.PhysicalDirectory, false) as HttpApplicationRequestQueue;
                if (applicationRequestQueue == null)
                {
                    throw new ApplicationException("Failed to load the request queue for processing HTTP requests.");
                }

                //get the app domain the target ASP.NET application will run in
                returnValue = ApplicationManager.GetAppDomain(args.Domain);

#if DEBUG
                //used for testing the ANTS framework
                applicationRequestQueue.SetTestingVariables(testingVariables);
#endif

                //set all the properties the request queue will need to process http requests
                applicationRequestQueue.ApplicationManager = ApplicationManager;
                applicationRequestQueue.AppType = typeof(THttpApplication);
                applicationRequestQueue.Domain = args.Domain;
                applicationRequestQueue.MaxThreads = args.ThreadCount;
                applicationRequestQueue.DefaultDomainWorker = DefaultDomainWorker;

                applicationRequestQueue.Init();

                //bootstrap the domain worker
                args.BootstrapDomainWorker(buildManagerHost, returnValue);

                //create the task that will complete when the app domain unloads (meaning the ASP.NET application has stopped)
                CloseTasks[args.Domain] = new TaskCompletionSource<object>();

                //start the ASP.NET application
                applicationRequestQueue.Start(args.FirstRouteToLoad ?? "");

                //add the queue to the list of ASP.NET applications that are running
                Applications[args.Domain] = applicationRequestQueue;

                args.InvokeAfterApplicationStartsOnDomainWorker();
            }

            return returnValue;
        }

        /// <summary>
        /// Manages ASP.NET application domains for an ASP.NET hosting application.
        /// </summary>
        public static ApplicationManager ApplicationManager { get; internal set; }

        /// <summary>
        /// Creates an <see cref="HttpClient"/> that sends http requests to the simulated server.
        /// </summary>
        /// <exception cref="ArgumentException">Unable to find the domain for this type.</exception>
        public static HttpClient GetHttpClient<THttpApplication>()
            where THttpApplication : HttpApplication, new()
        {
            var domain = DefaultDomainWorker?.GetDomainFromType(typeof(THttpApplication).FullName);
            if (string.IsNullOrEmpty(domain))
            {
                throw new ArgumentException("Unable to find the domain for this type.");
            }
            return GetHttpClient(domain);
        }

        /// <summary>
        /// Creates an <see cref="HttpClient"/> that sends http requests to the simulated server.
        /// </summary>
        /// <param name="domain">Optional. The domain for the ASP.NET application.</param>
        public static HttpClient GetHttpClient(string domain = null)
        {
            var returnValue = new HttpClient(HttpMessageHandler);

            if (string.IsNullOrEmpty(domain))
            {
                return returnValue;
            }

            returnValue.BaseAddress = new Uri($"http://{domain}/");
            returnValue.DefaultRequestHeaders.Host = domain;

            return returnValue;
        }

        /// <summary>
        /// Gets the <see cref="HttpMessageHandler"/> used by <see cref="HttpClient"/>s to send http requests to the simulated server.
        /// </summary>
        public static HttpMessageHandler HttpMessageHandler { get; } = new HttpClientTestServerHandler();

        /// <summary>
        /// Stops hosting an ASP.NET application in the simulated server.
        /// </summary>
        /// <exception cref="InvalidOperationException">The test server can only be accessed from the default domain.</exception>
        /// <typeparam name="THttpApplication">The type for the ASP.NET application.</typeparam>
        public static Task Stop<THttpApplication>()
            where THttpApplication : HttpApplication, new()
        {
            throwIfNotDefaultAppDomain();

            var app = GetApplication<THttpApplication>();
            if (app == null ||
                !Applications.TryRemove(app.Domain, out app) ||
                !CloseTasks.TryGetValue(app.Domain, out TaskCompletionSource<object> onCloseTask))
            {
                return Task.FromResult(0);
            }

            app.Stop(true);

            ApplicationManager.ShutdownApplication(app.Domain);

            return onCloseTask.Task;
        }

        /// <summary>
        /// Get or sets if the current domain should be treated as the default app domain.
        /// When testing you need to set this to true because tests do not run in the default app domain.
        /// </summary>
        public static bool IsDefaultAppDomain { get; set; } = AppDomain.CurrentDomain.IsDefaultAppDomain();
    }
}

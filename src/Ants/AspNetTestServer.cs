using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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
        internal static readonly ConcurrentDictionary<string, HttpApplicationRequestQueue> Applications = new ConcurrentDictionary<string, HttpApplicationRequestQueue>(StringComparer.OrdinalIgnoreCase);
        internal static readonly ConcurrentDictionary<string, TaskCompletionSource<object>> CloseTasks = new ConcurrentDictionary<string, TaskCompletionSource<object>>(StringComparer.OrdinalIgnoreCase);

        private static DefaultDomainWorker worker;
        private static HttpApplicationRequestQueue GetApplication<THttpApplication>()
            where THttpApplication : HttpApplication, new()
        {
            return Applications.Values.FirstOrDefault(queue => queue.AppType == typeof(THttpApplication));
        }
        private static readonly ApplicationManager applicationManager = ApplicationManager.GetApplicationManager();
        private static readonly Type buildManagerHostType = typeof(HttpRuntime).Assembly.GetType("System.Web.Compilation.BuildManagerHost");
        private static void throwIfNotDefaultAppDoamin()
        {
            if (!IsDefaultAppDomain)
            {
                throw new InvalidOperationException("The test server can only be accessed from the default domain.");
            }

            worker = worker ?? new DefaultDomainWorker();
        }

        /// <summary>
        /// Starts hosting an ASP.NET application in a simulated server.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="IndexOutOfRangeException"></exception>
        /// <exception cref="InvalidOperationException">The test server can only be accessed from the default domain.</exception>
        /// <typeparam name="THttpApplication">The type for the ASP.NET application.</typeparam>
        /// <param name="args">Optional. The arguments for starting an ASP.NET application.</param>
        /// <returns>The <see cref="AppDomain"/> the ASP.NET application runs in.</returns>
        public static AppDomain Start<THttpApplication>(StartApplicationArgs args = null)
            where THttpApplication : HttpApplication, new()
        {
            throwIfNotDefaultAppDoamin();

            args = args ?? new StartApplicationArgs();

            AppDomain returnValue;

            lock (args) //make the args immutable
            {
#if DEBUG
                var testingVariables = Testing.Variables;
#endif

                args.Sanitize<THttpApplication>();

                args.ThrowOnValidationError();

                //by adding a null value this makes setting up apps thread safe
                if (!Applications.TryAdd(args.Domain, null))
                {
                    throw new InvalidOperationException("An ASP.NET application is already running at this domain: " + args.Domain);
                }

                //create the app domain for the ASP.NET application to run
                var buildManagerHost = applicationManager.CreateObject(args.Domain, buildManagerHostType, args.VirtualDirectory, args.PhysicalDirectory, true);

                var applicationRequestQueueType = typeof(HttpApplicationRequestQueue);
                buildManagerHostType.InvokeMember("RegisterAssembly",
                                                  BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic,
                                                  null,
                                                  buildManagerHost,
                                                  new object[] { applicationRequestQueueType.Assembly.FullName, applicationRequestQueueType.Assembly.Location });

                if (args.AssembliesToLoad != null)
                {
                    foreach (var assembly in args.AssembliesToLoad)
                    {
                        buildManagerHostType.InvokeMember("RegisterAssembly",
                                                          BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic,
                                                          null,
                                                          buildManagerHost,
                                                          new object[] { assembly.FullName, assembly.Location });
                    }
                }

                //create the ASP.NET application message queue 
                var applicationRequestQueue = applicationManager.CreateObject(args.Domain, applicationRequestQueueType, args.VirtualDirectory, args.PhysicalDirectory, false) as HttpApplicationRequestQueue;
                if (applicationRequestQueue == null)
                {
                    throw new ApplicationException("Failed to load the request queue for processing HTTP requests.");
                }

                returnValue = applicationManager.GetAppDomain(args.Domain);

#if DEBUG
                applicationRequestQueue.SetTestingVariables(testingVariables);
#endif

                args.PreInitialize?.Invoke(returnValue);

                applicationRequestQueue.AppType = typeof(THttpApplication);
                applicationRequestQueue.Domain = args.Domain;
                applicationRequestQueue.MaxThreads = args.ThreadCount;
                applicationRequestQueue.Worker = worker;

                CloseTasks[args.Domain] = new TaskCompletionSource<object>();

                applicationRequestQueue.Init(args.FirstRouteToLoad ?? "");

                Applications[args.Domain] = applicationRequestQueue;
            }

            return returnValue;
        }

        /// <summary>
        /// Creates an <see cref="HttpClient"/> that sends http requests to the simulated server.
        /// </summary>
        /// <exception cref="ArgumentException">Unable to find the domain for this type.</exception>
        /// <exception cref="InvalidOperationException">The test server can only be accessed from the default domain.</exception>
        public static HttpClient GetHttpClient<THttpApplication>()
            where THttpApplication : HttpApplication, new()
        {
            var domain = GetApplication<THttpApplication>()?.Domain;
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
        /// <exception cref="InvalidOperationException">The test server can only be accessed from the default domain.</exception>
        public static HttpClient GetHttpClient(string domain = null)
        {
            throwIfNotDefaultAppDoamin();

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
        /// Get or sets if the current domain should be treated as the default app domain.
        /// When testing you need to set this to true because tests do not run in the default app domain.
        /// </summary>
        public static bool IsDefaultAppDomain { get; set; } = AppDomain.CurrentDomain.IsDefaultAppDomain();

        /// <summary>
        /// Stops hosting an ASP.NET application in the simulated server.
        /// </summary>
        /// <typeparam name="THttpApplication">The type for the ASP.NET application.</typeparam>
        public static Task Stop<THttpApplication>()
            where THttpApplication : HttpApplication, new()
        {
            var app = GetApplication<THttpApplication>();
            if (app == null ||
                !Applications.TryRemove(app.Domain, out app) ||
                !CloseTasks.TryGetValue(app.Domain, out TaskCompletionSource<object> onCloseTask))
            {
                return Task.FromResult(0);
            }

            app.Stop(true);

            applicationManager.ShutdownApplication(app.Domain);

            return onCloseTask.Task;
        }
    }
}

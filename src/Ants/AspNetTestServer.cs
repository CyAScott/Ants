using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using Ants.AutoLoader;
using Ants.HttpRequestQueue;

namespace Ants
{
    /// <summary>
    /// A static class for managing ASP.NET applications running on a simulated server.
    /// </summary>
    public static partial class AspNetTestServer
    {
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
        /// <param name="assembly">The assembly for the ASP.NET application.</param>
        /// <param name="args">Optional. The arguments for starting an ASP.NET application.</param>
        /// <returns>The <see cref="AppDomain"/> the ASP.NET application runs in.</returns>
        public static AppDomain Start(Assembly assembly, StartApplicationArgs args = null)
        {
            return Start(assembly.GetGuid(), args);
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
        /// <param name="id">The ID for the ASP.NET application.</param>
        /// <param name="args">Optional. The arguments for starting an ASP.NET application.</param>
        /// <returns>The <see cref="AppDomain"/> the ASP.NET application runs in.</returns>
        public static AppDomain Start(Guid id, StartApplicationArgs args = null)
        {
            throwIfNotDefaultAppDomain();

            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            args = args ?? new StartApplicationArgs();

            AppDomain appDomain;

            lock (args)
            {
#if DEBUG
                //test variables used to test the ANTS framework
                var testingVariables = Testing.Variables;
#endif

                args.Sanitize(id);

                args.ThrowOnValidationError();

                //by adding a null value this makes setting up apps thread safe
                if (!Applications.TryAdd(args.Domain, null))
                {
                    throw new InvalidOperationException("An ASP.NET application is already running.");
                }

                //create the app domain for the ASP.NET application to run on
                var buildManagerHost = ApplicationManager.CreateObject(args.Domain, Extensions.BuildManagerHostType, args.VirtualDirectory, args.PhysicalDirectory, true);

                //get the app domain the target ASP.NET application will run in
                appDomain = ApplicationManager.GetAppDomain(args.Domain);
                appDomain.SetData("Ants.Domain", args.Domain);

                //register the assembly for ANTS for the target app domain
                var applicationRequestQueueType = typeof(HttpApplicationRequestQueue);
                buildManagerHost.RegisterAssembly(appDomain, applicationRequestQueueType.Assembly);

                //register auto loaded assemblies
                var autoLoadAssemblyHelpers = new List<AutoLoadAssemblyHelper>();
                foreach (var tuple in AutoLoadAssemblies.Value.Where(tuple => !tuple.Item2.LoadAfterOtherAssemblies))
                {
                    buildManagerHost.RegisterAssembly(appDomain, tuple.Item1);
                    if (tuple.Item2.AutoLoadAssemblyHelper != null &&
                        typeof(AutoLoadAssemblyHelper).IsAssignableFrom(tuple.Item2.AutoLoadAssemblyHelper))
                    {
                        autoLoadAssemblyHelpers.Add((AutoLoadAssemblyHelper)appDomain.CreateInstanceAndUnwrap(tuple.Item1.FullName, tuple.Item2.AutoLoadAssemblyHelper.FullName ?? ""));
                    }
                }

                //register any assemblies provided by the developer
                if (args.AssembliesToLoad != null)
                {
                    foreach (var assembly in args.AssembliesToLoad
                        .Where(assembly => assembly != applicationRequestQueueType.Assembly))
                    {
                        buildManagerHost.RegisterAssembly(appDomain, assembly);
                    }
                }

                //register auto loaded assemblies
                foreach (var tuple in AutoLoadAssemblies.Value.Where(tuple => tuple.Item2.LoadAfterOtherAssemblies))
                {
                    buildManagerHost.RegisterAssembly(appDomain, tuple.Item1);
                    if (tuple.Item2.AutoLoadAssemblyHelper != null &&
                        typeof(AutoLoadAssemblyHelper).IsAssignableFrom(tuple.Item2.AutoLoadAssemblyHelper))
                    {
                        autoLoadAssemblyHelpers.Add((AutoLoadAssemblyHelper)appDomain.CreateInstanceAndUnwrap(tuple.Item1.FullName, tuple.Item2.AutoLoadAssemblyHelper.FullName ?? ""));
                    }
                }

                //create the ASP.NET application message queue for processing http requests
                var applicationRequestQueue = ApplicationManager.CreateObject(args.Domain, applicationRequestQueueType, args.VirtualDirectory, args.PhysicalDirectory, false) as HttpApplicationRequestQueue;
                if (applicationRequestQueue == null)
                {
                    throw new ApplicationException("Failed to load the request queue for processing HTTP requests.");
                }

#if DEBUG
                //used for testing the ANTS framework
                applicationRequestQueue.SetTestingVariables(testingVariables);
#endif

                //set all the properties the request queue will need to process http requests
                applicationRequestQueue.ApplicationManager = ApplicationManager;
                applicationRequestQueue.Id = id;
                applicationRequestQueue.Domain = args.Domain;
                applicationRequestQueue.Helpers = autoLoadAssemblyHelpers.ToArray();
                applicationRequestQueue.MaxThreads = args.ThreadCount;
                applicationRequestQueue.DefaultDomainWorker = DefaultDomainWorker;
                applicationRequestQueue.StartApplicationArgs = args;

                applicationRequestQueue.Init();

                //bootstrap the domain worker
                args.BootstrapDomainWorker(buildManagerHost, appDomain);

                //create the task that will complete when the app domain unloads (meaning the ASP.NET application has stopped)
                CloseTasks[args.Domain] = new TaskCompletionSource<object>();

                //execute auto loaded assembly code before the application starts
                foreach (var helper in autoLoadAssemblyHelpers)
                {
                    helper.BeforeFirstRouteLoad();
                }

                //add the queue to the list of ASP.NET applications that are running
                Applications[args.Domain] = applicationRequestQueue;

                //start the ASP.NET application
                if (args.FirstRouteToLoad != null)
                {
                    using (var client = GetHttpClient(args.Domain))
                    using (var task = client.GetAsync(args.FirstRouteToLoad))
                    {
                        task.Wait();
                        using (var results = task.Result)
                        using (var resultsContent = results.Content)
                        {
#if DEBUG
                            using (var readTask = resultsContent.ReadAsStringAsync())
                            {
                                readTask.Wait();
                                Debug.WriteLine(readTask.Result);
                            }
#endif
                        }
                    }
                }
            }

            return appDomain;
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
            return Start(typeof(THttpApplication).GUID, args);
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
            var domain = DefaultDomainWorker?.GetDomainFromId(typeof(THttpApplication).GUID);
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
        /// This handler keeps track of cookies by default.
        /// </summary>
        public static HttpClientTestServerHandler HttpMessageHandler { get; } = new HttpClientTestServerHandler();

        /// <summary>
        /// Stops hosting an ASP.NET application in the simulated server.
        /// </summary>
        /// <exception cref="InvalidOperationException">The test server can only be accessed from the default domain.</exception>
        public static Task Stop(Assembly assembly)
        {
            return Stop(assembly.GetGuid());
        }

        /// <summary>
        /// Stops hosting an ASP.NET application in the simulated server.
        /// </summary>
        /// <exception cref="InvalidOperationException">The test server can only be accessed from the default domain.</exception>
        public static Task Stop(Guid id)
        {
            throwIfNotDefaultAppDomain();

            var app = GetApplication(id);
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
        /// Stops hosting an ASP.NET application in the simulated server.
        /// </summary>
        /// <exception cref="InvalidOperationException">The test server can only be accessed from the default domain.</exception>
        /// <typeparam name="THttpApplication">The type for the ASP.NET application.</typeparam>
        public static Task Stop<THttpApplication>()
            where THttpApplication : HttpApplication, new()
        {
            return Stop(typeof(THttpApplication).GUID);
        }

        /// <summary>
        /// Stops hosting all ASP.NET applications in the simulated server.
        /// </summary>
        /// <exception cref="InvalidOperationException">The test server can only be accessed from the default domain.</exception>
        public static async Task StopAll()
        {
            throwIfNotDefaultAppDomain();

            foreach (var app in Applications.Values.ToArray())
            {
                if (!Applications.TryRemove(app.Domain, out HttpApplicationRequestQueue application) ||
                    !CloseTasks.TryGetValue(application.Domain, out TaskCompletionSource<object> onCloseTask))
                {
                    continue;
                }

                application.Stop(true);

                ApplicationManager.ShutdownApplication(application.Domain);

                await onCloseTask.Task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get or sets if the current domain should be treated as the default app domain.
        /// When testing you need to set this to true because tests do not run in the default app domain.
        /// </summary>
        public static bool IsDefaultAppDomain { get; set; } = AppDomain.CurrentDomain.IsDefaultAppDomain();

        /// <summary>
        /// Sets the list of auto loaded assemblies that have the <see cref="AutoLoadIntoAntsAttribute"/> attribute.
        /// </summary>
        public static void SetAutoLoadAssemblies(params Assembly[] assemblies)
        {
            throwIfNotDefaultAppDomain();

            AutoLoadAssemblies = new Lazy<Tuple<Assembly, AutoLoadIntoAntsAttribute>[]>(() => getAutoLoadAssemblies(assemblies));
            if (AutoLoadAssemblies.Value == null)
            {
                throw new InvalidDataException("Unable to set the auto load assemblies.");
            }
        }
    }
}

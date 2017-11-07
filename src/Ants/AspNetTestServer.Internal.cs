using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Hosting;
using Ants.AutoLoader;
using Ants.HttpRequestQueue;

namespace Ants
{
    public static partial class AspNetTestServer
    {
        internal static DefaultDomainWorker DefaultDomainWorker { get; set; }
        internal static HttpApplicationRequestQueue GetApplication(Guid id)
        {
            return Applications.Values.FirstOrDefault(queue => queue.Id == id);
        }
        internal static Lazy<Tuple<Assembly, AutoLoadIntoAntsAttribute>[]> AutoLoadAssemblies = new Lazy<Tuple<Assembly, AutoLoadIntoAntsAttribute>[]>(() => getAutoLoadAssemblies());
        internal static readonly ConcurrentDictionary<string, HttpApplicationRequestQueue> Applications = new ConcurrentDictionary<string, HttpApplicationRequestQueue>(StringComparer.OrdinalIgnoreCase);
        internal static readonly ConcurrentDictionary<string, TaskCompletionSource<object>> CloseTasks = new ConcurrentDictionary<string, TaskCompletionSource<object>>(StringComparer.OrdinalIgnoreCase);

        private static Tuple<Assembly, AutoLoadIntoAntsAttribute>[] getAutoLoadAssemblies(params Assembly[] assemblies)
        {
            return assemblies
                .Concat(AppDomain
                    .CurrentDomain
                    .GetAssemblies())
                .Concat(new[]
                {
                    Assembly.GetCallingAssembly(),
                    Assembly.GetEntryAssembly(),
                    Assembly.GetExecutingAssembly()
                })
                .Where(assembly => assembly != null)
                .SelectMany(assembly => assembly
                    .GetReferencedAssemblies()
                    .Select(assemblyName => assemblyName.FullName)
                    .Concat(new[] { assembly.FullName }))
                .Distinct()
                .OrderBy(assemblyName => assemblyName)
                .Select(assemblyName =>
                {
                    try
                    {
                        var assembly = Assembly.Load(assemblyName.ToString());
                        var attribute = assembly?.GetCustomAttribute<AutoLoadIntoAntsAttribute>();
                        return attribute == null ? null : new Tuple<Assembly, AutoLoadIntoAntsAttribute>(assembly, attribute);
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(tuple => tuple != null)
                .ToArray();
        }
        private static void throwIfNotDefaultAppDomain()
        {
            if (!IsDefaultAppDomain)
            {
                throw new InvalidOperationException("The test server can only be accessed from the default domain.");
            }

            ApplicationManager = ApplicationManager ?? ApplicationManager.GetApplicationManager();
            DefaultDomainWorker = DefaultDomainWorker ?? new DefaultDomainWorker();
        }
    }
}

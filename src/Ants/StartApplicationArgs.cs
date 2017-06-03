using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;

namespace Ants
{
    /// <summary>
    /// The arguments for starting an ASP.NET application.
    /// </summary>
    public class StartApplicationArgs
    {
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

        internal void Sanitize<THttpApplication>()
            where THttpApplication : HttpApplication, new()
        {
            if (string.IsNullOrEmpty(Domain))
            {
                Domain = typeof(THttpApplication).GUID.ToString();
            }
        }

        /// <summary>
        /// Optional. Code that is invoked after the assemblies are loaded into the <see cref="AppDomain"/> but before the first route is loaded.
        /// This code runs in the default domain.
        /// The argument is a reference to the <see cref="AppDomain"/> the ASP.NET application will run in.
        /// </summary>
        public Action<AppDomain> PreInitialize { get; set; }

        /// <summary>
        /// Optional. Assemblies to laod into the <see cref="AppDomain"/> that houses the ASP.NET application.
        /// </summary>
        public IEnumerable<Assembly> AssembliesToLoad { get; set; }

        /// <summary>
        /// The max number of threads for processing requests.
        /// </summary>
        public int ThreadCount { get; set; } = 1;

        /// <summary>
        /// The domain you want to use for the simulation.
        /// If no value is provided then the GUID for the <see cref="HttpApplication"/> type is used.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// The first route to load to start the ASP.NET application.
        /// The default value is <see cref="string.Empty"/>.
        /// </summary>
        public string FirstRouteToLoad { get; set; } = string.Empty;

        /// <summary>
        /// The project directory where the ASP.NET application.
        /// This folder is used to host the simulated server just like IIS or IIS express does for hosting the site.
        /// </summary>
        public string PhysicalDirectory { get; set; }

        /// <summary>
        /// The virtual directory is the path in the url proceeding all the possible file paths in the physical directory.
        /// The default value is "/".
        /// </summary>
        public string VirtualDirectory { get; set; } = "/";
    }
}

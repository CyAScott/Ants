using System;

namespace Ants.AutoLoader
{
    /// <summary>
    /// A helper class that executes code on the <see cref="AppDomain"/> of the Ants instance.
    /// </summary>
    public abstract class AutoLoadAssemblyHelper : MarshalByRefObject
    {
        /// <summary>
        /// Executes after the first route is loaded.
        /// </summary>
        public virtual void AfterFirstRouteLoaded()
        {
        }

        /// <summary>
        /// Executes after the domain worker starts but before the first route is loaded.
        /// </summary>
        public virtual void BeforeFirstRouteLoad()
        {
        }
    }
}

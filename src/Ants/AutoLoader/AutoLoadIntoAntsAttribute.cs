using System;

namespace Ants.AutoLoader
{
    /// <summary>
    /// An assembly attribute used to indicate that the assembly needs to be loaded into the <see cref="AppDomain"/> for all Ants instances.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class AutoLoadIntoAntsAttribute : Attribute
    {
        /// <summary>
        /// A helper class that executes code on the <see cref="AppDomain"/> of the Ants instance.
        /// The type must be derived from <see cref="AutoLoadAssemblyHelper"/>.
        /// </summary>
        public Type AutoLoadAssemblyHelper { get; set; }

        /// <summary>
        /// If true, the assembly is loaded after the developer provided assemblies are, else it is loaded before.
        /// </summary>
        public bool LoadAfterOtherAssemblies { get; set; }
    }
}

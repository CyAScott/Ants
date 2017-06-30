using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Web;
using Ants.Owin;

namespace Ants
{
    /// <summary>
    /// A class for helping with HTTP module loading.
    /// </summary>
    public static class HttpModuleHelper
    {
        static HttpModuleHelper()
        {
            var dynamicModuleRegistry = typeof(HttpApplication)
                .GetField("_dynamicModuleRegistry", BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetValue(null);
            if (dynamicModuleRegistry == null)
            {
                throw new InvalidProgramException("Unable to read the _dynamicModuleRegistry field.");
            }

            var dynamicModuleRegistryType = dynamicModuleRegistry.GetType();
            dynamicModuleRegistryEntries = dynamicModuleRegistryType
                .GetField("_entries", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(dynamicModuleRegistry) as IList;
            if (dynamicModuleRegistryEntries == null)
            {
                throw new InvalidProgramException("Unable to read the _entries field.");
            }

            var dynamicModuleInfoType = dynamicModuleRegistryEntries
                .GetType()
                .GetGenericArguments()
                .First();
            dynamicModuleInfoCtor = dynamicModuleInfoType.GetConstructor(new[]
            {
                typeof(string), //name
                typeof(string)  //type (AssemblyQualifiedName)
            });
            if (dynamicModuleInfoCtor == null)
            {
                throw new InvalidProgramException("Unable to read the ctor for the DynamicModuleRegistryEntry.");
            }

            dynamicModuleInfoNameField = dynamicModuleInfoType.GetField("Name", BindingFlags.Instance | BindingFlags.Public);
            if (dynamicModuleInfoNameField == null)
            {
                throw new InvalidProgramException("Unable to read the Name field for the DynamicModuleRegistryEntry.");
            }

            dynamicModuleInfoTypeField = dynamicModuleInfoType.GetField("Type", BindingFlags.Instance | BindingFlags.Public);
            if (dynamicModuleInfoTypeField == null)
            {
                throw new InvalidProgramException("Unable to read the Type field for the DynamicModuleRegistryEntry.");
            }
        }

        internal static void ReplaceOwinHttpModule()
        {
            try
            {
                var owinHttpModule = Assembly.Load("Microsoft.Owin.Host.SystemWeb").GetType("Microsoft.Owin.Host.SystemWeb.OwinHttpModule");
                if (owinHttpModule != null)
                {
                    ReplaceHttpModules(owinHttpModule, typeof(OwinHttpModuleWrapper<>).MakeGenericType(owinHttpModule));
                }
            }
            catch
            {
                // ignored
            }
        }

        private class DynamicModuleRegistryEntry
        {
            private readonly string name;

            public DynamicModuleRegistryEntry(object entry)
            {
                name = dynamicModuleInfoNameField.GetValue(entry) as string;
                Type = Type.GetType(dynamicModuleInfoTypeField.GetValue(entry) as string ?? "", false);
            }
            public object ToDynamicModuleRegistryEntry()
            {
                return dynamicModuleInfoCtor.Invoke(new object[]
                {
                    name,
                    Type.AssemblyQualifiedName
                });
            }
            public Type Type { get; set; }
        }
        private static readonly ConstructorInfo dynamicModuleInfoCtor;
        private static readonly FieldInfo dynamicModuleInfoNameField, dynamicModuleInfoTypeField;
        private static readonly IList dynamicModuleRegistryEntries;

        /// <summary>
        /// Replaces a registered http module with another one.
        /// </summary>
        public static void ReplaceHttpModules(Type oldModule, Type newModule)
        {
            lock (dynamicModuleRegistryEntries)
            {
                foreach (var oldDynamicModuleInfo in dynamicModuleRegistryEntries
                    .Cast<object>()
                    .Select((dynamicModuleInfo, index) => new
                    {
                        index = index,
                        value = new DynamicModuleRegistryEntry(dynamicModuleInfo)
                    })
                    .Where(oldDynamicModuleInfo => oldDynamicModuleInfo.value.Type == oldModule)
                    .ToArray())
                {
                    oldDynamicModuleInfo.value.Type = newModule;
                    dynamicModuleRegistryEntries[oldDynamicModuleInfo.index] = oldDynamicModuleInfo.value.ToDynamicModuleRegistryEntry();
                }
            }
        }
    }
}

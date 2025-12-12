using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ArchiGraph
{
    public static class TypeScanner
    {
        public static List<Type> GetProjectTypes()
        {
            var result = new List<Type>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !IsIgnoredAssembly(a));

            foreach (var asm in assemblies)
            {
                try
                {
                    result.AddRange(
                        asm.GetTypes()
                            .Where(t => (t.IsClass || t.IsInterface) && t.IsPublic && !t.IsNested)
                    );
                }
                catch (ReflectionTypeLoadException e)
                {
                    if (e.Types == null)
                        continue;

                    result.AddRange(
                        e.Types
                            .Where(t => t != null && (t.IsClass || t.IsInterface) && t.IsPublic && !t.IsNested)
                    );
                }
            }

            return result;
        }

        private static bool IsIgnoredAssembly(Assembly asm)
        {
            string name = asm.GetName().Name;

            if (string.IsNullOrEmpty(name))
                return true;

            return
                name.StartsWith("System") ||
                name.StartsWith("mscorlib") ||
                name.StartsWith("netstandard") ||
                name.StartsWith("UnityEngine") ||
                name.StartsWith("UnityEditor") ||
                name.StartsWith("Unity.") ||
                name.StartsWith("Mono.") ||
                name.StartsWith("Microsoft.") ||
                name.StartsWith("nunit", StringComparison.OrdinalIgnoreCase);
        }
    }
}

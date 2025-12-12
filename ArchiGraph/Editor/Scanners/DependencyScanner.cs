using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ArchiGraph
{
    public static class DependencyScanner
    {
        public static List<Type> GetClassDependencies(Type type, List<Type> validTypes)
        {
            var deps = new HashSet<Type>();

            BindingFlags flags = BindingFlags.Instance |
                                 BindingFlags.Public |
                                 BindingFlags.NonPublic |
                                 BindingFlags.Static |
                                 BindingFlags.DeclaredOnly;

            foreach (var f in type.GetFields(flags))
                Collect(f.FieldType);

            foreach (var p in type.GetProperties(flags))
                Collect(p.PropertyType);

            foreach (var c in type.GetConstructors(flags))
                foreach (var param in c.GetParameters())
                    Collect(param.ParameterType);

            foreach (var m in type.GetMethods(flags))
            {
                if (m.IsSpecialName)
                    continue;

                Collect(m.ReturnType);

                foreach (var param in m.GetParameters())
                    Collect(param.ParameterType);
            }

            void Collect(Type t)
            {
                if (t == null)
                    return;

                if (t.IsGenericType)
                {
                    foreach (var arg in t.GetGenericArguments())
                        Collect(arg);
                }

                if (t.IsArray)
                {
                    Collect(t.GetElementType());
                    return;
                }

                if (validTypes.Contains(t))
                    deps.Add(t);
            }

            deps.Remove(type);
            return deps.ToList();
        }
    }
}

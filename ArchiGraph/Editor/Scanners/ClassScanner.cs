using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ArchiGraph
{
    public static class ClassScanner
    {
        public static List<Type> GetTypesFromFolder(string folderPath)
        {
            var result = new List<Type>();

            string[] guids = AssetDatabase.FindAssets("t:Script", new[] { folderPath });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null)
                    continue;

                var type = script.GetClass();
                if (type == null)
                    continue;

                if (!(type.IsClass || type.IsInterface))
                    continue;

                if (type.IsNested)
                    continue;

                result.Add(type);
            }

            return result.Distinct().ToList();
        }
    }
}
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CustomPropertyReferenceAttribute : Attribute
    {
        public static List<TBase> CreateAllWithAttribute<TAttribute, TBase>()
            where TAttribute : Attribute
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t =>
                    typeof(TBase).IsAssignableFrom(t) &&
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.GetCustomAttribute<TAttribute>() != null)
                .Select(t => (TBase)Activator.CreateInstance(t))
                .ToList();
        }

        public static List<object> CreateAllWithAttribute<TAttribute>()
            where TAttribute : Attribute
        {
            var assembly = Assembly.GetExecutingAssembly();

            var types = assembly.GetTypes()
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.GetCustomAttribute<TAttribute>() != null);

            var instances = new List<object>();

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);
                instances.Add(instance);
            }

            return instances;
        }

        public static List<object> CreateAllWithAttributeFromAllAssemblies<TAttribute>()
            where TAttribute : Attribute
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var types = assemblies
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.GetCustomAttribute<TAttribute>() != null);

            var instances = new List<object>();

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);
                instances.Add(instance);
            }

            return instances;
        }
    }
}
#endif
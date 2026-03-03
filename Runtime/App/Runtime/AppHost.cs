using System;
using System.Collections.Generic;
using System.Linq;
using Snm.App.DependencyInjection;

namespace Snm.App.Runtime
{
    public sealed class AppHost
    {
        private readonly IResolver resolver;

        public AppHost(IResolver resolver)
        {
            this.resolver = resolver;
        }

        public void Start()
        {
            var runtimeRoots = resolver.ResolveAll<IRuntimeRoot>();

            var sorted = TopologicalSort(runtimeRoots);

            foreach (var root in sorted)
            {
                root.Start();
            }
        }

        public void Stop()
        {
            (resolver as IDisposable)?.Dispose();
        }
        
        private static List<IRuntimeRoot> TopologicalSort(
            IEnumerable<IRuntimeRoot> roots)
        {
            var rootList = roots.ToList();

            var typeToRoot = rootList.ToDictionary(r => r.GetType());

            // Build graph
            var incomingEdges = new Dictionary<Type, int>();
            var adjacency = new Dictionary<Type, List<Type>>();

            foreach (var root in rootList)
            {
                var type = root.GetType();
                incomingEdges[type] = 0;
                adjacency[type] = new List<Type>();
            }

            foreach (var root in rootList)
            {
                var type = root.GetType();

                foreach (var dep in root.Dependencies ?? Array.Empty<Type>())
                {
                    if (!typeToRoot.ContainsKey(dep))
                        throw new InvalidOperationException(
                            $"{type.Name} depends on {dep.Name}, but it is not registered as IRuntimeRoot.");

                    adjacency[dep].Add(type);
                    incomingEdges[type]++;
                }
            }

            // Queue nodes with no incoming edges
            var queue = new List<Type>(
                incomingEdges
                    .Where(kv => kv.Value == 0)
                    .Select(kv => kv.Key)
            );

            // Optional: deterministic order inside same level
            queue.Sort((a, b) =>
                typeToRoot[a].Order.CompareTo(typeToRoot[b].Order));

            var result = new List<IRuntimeRoot>();

            while (queue.Count > 0)
            {
                var current = queue[0];
                queue.RemoveAt(0);

                result.Add(typeToRoot[current]);

                foreach (var neighbor in adjacency[current])
                {
                    incomingEdges[neighbor]--;

                    if (incomingEdges[neighbor] == 0)
                    {
                        queue.Add(neighbor);

                        queue.Sort((a, b) =>
                            typeToRoot[a].Order.CompareTo(typeToRoot[b].Order));
                    }
                }
            }

            if (result.Count != rootList.Count)
                throw new InvalidOperationException(
                    "Circular dependency detected among IRuntimeRoot implementations.");

            return result;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.App.DependencyInjection;

namespace Snm.Runtime.App.Lifecycle
{
    // ----------------------------
    // Lifecycle Service
    // ----------------------------
    public sealed class LifecycleService
    {
        private readonly IResolver _resolver;

        private List<IInitializable> _initializables;
        private List<IStartable> _startables;
        private List<IStoppable> _stoppables;

        private bool _initialized;
        private bool _started;

        public LifecycleService(IResolver resolver)
        {
            _resolver = resolver;
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            _initializables = TopologicalSort(_resolver.ResolveAll<IInitializable>());
            _startables = TopologicalSort(_resolver.ResolveAll<IStartable>());
            _stoppables = TopologicalSort(_resolver.ResolveAll<IStoppable>());

            foreach (var i in _initializables)
                i.Initialize();

            _initialized = true;
        }

        public void Start()
        {
            if (_started)
                return;

            foreach (var s in _startables)
                s.Start();

            _started = true;
        }

        public void Stop()
        {
            if (!_started)
                return;

            // Reverse order for safe teardown
            for (int i = _stoppables.Count - 1; i >= 0; i--)
                _stoppables[i].Stop();

            _started = false;
        }

        private static List<T> TopologicalSort<T>(IEnumerable<T> items)
        {
            var itemList = items.ToList();
            var result = new List<T>();

            var dependencyMap = new Dictionary<T, HashSet<T>>();
            var reverseMap = new Dictionary<T, HashSet<T>>();

            foreach (var item in itemList)
            {
                dependencyMap[item] = new HashSet<T>();
                reverseMap[item] = new HashSet<T>();
            }

            foreach (var item in itemList)
            {
                if (item is IDependentLifecycle dependent)
                {
                    foreach (var depType in dependent.Dependencies)
                    {
                        var dependency = itemList
                            .FirstOrDefault(x => depType.IsAssignableFrom(x.GetType()));

                        if (dependency == null)
                            throw new InvalidOperationException(
                                $"Missing lifecycle dependency: {depType.Name}");

                        dependencyMap[item].Add(dependency);
                        reverseMap[dependency].Add(item);
                    }
                }
            }

            var queue = new Queue<T>(
                dependencyMap.Where(x => x.Value.Count == 0)
                             .Select(x => x.Key));

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                result.Add(node);

                foreach (var dependent in reverseMap[node])
                {
                    dependencyMap[dependent].Remove(node);
                    if (dependencyMap[dependent].Count == 0)
                        queue.Enqueue(dependent);
                }
            }

            if (result.Count != itemList.Count)
                throw new InvalidOperationException("Circular lifecycle dependency detected.");

            return result;
        }
    }
}
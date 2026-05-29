using System;
using System.Collections.Generic;
using System.Linq;
using Snm.DependencyInjection;

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

            // Resolve all lifecycle slices, then build a union for cross-list dependency lookup.
            // One object may implement multiple lifecycle interfaces and may have dependencies on
            // objects in a different slice (e.g. an IStartable that depends on an IInitializable).
            // Cross-slice deps don't impose ordering within the current slice, but must exist somewhere.
            var initializables = _resolver.ResolveAllLocal<IInitializable>();
            var startables = _resolver.ResolveAllLocal<IStartable>();
            var stoppables = _resolver.ResolveAllLocal<IStoppable>();

            var union = new HashSet<object>();
            foreach (var x in initializables) union.Add(x);
            foreach (var x in startables) union.Add(x);
            foreach (var x in stoppables) union.Add(x);

            _initializables = TopologicalSort(initializables, union);
            _startables = TopologicalSort(startables, union);
            _stoppables = TopologicalSort(stoppables, union);

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
            // Run teardown if Initialize succeeded, regardless of whether Start completed.
            // If Start threw mid-way, IStoppables still need to release resources they acquired
            // during Initialize — skipping Stop would leak them.
            if (!_initialized)
                return;

            // Reverse order for safe teardown
            for (int i = _stoppables.Count - 1; i >= 0; i--)
                _stoppables[i].Stop();

            _started = false;
            _initialized = false;
        }

        private static List<T> TopologicalSort<T>(IEnumerable<T> items, HashSet<object> lifecycleUnion)
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
                        // Resolve the dependency against this slice first; only it can impose ordering here.
                        var dependency = itemList
                            .FirstOrDefault(x => depType.IsAssignableFrom(x.GetType()));

                        if (dependency != null)
                        {
                            dependencyMap[item].Add(dependency);
                            reverseMap[dependency].Add(item);
                            continue;
                        }

                        // Cross-slice dependency: must exist in the lifecycle union (any phase),
                        // otherwise the dependency is genuinely missing.
                        var existsInUnion = lifecycleUnion.Any(x => depType.IsAssignableFrom(x.GetType()));
                        if (!existsInUnion)
                            throw new InvalidOperationException(
                                $"Missing lifecycle dependency: {depType.Name}");
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
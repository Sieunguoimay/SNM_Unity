using System;
using System.Collections.Generic;
using System.Linq;

namespace Snm.App.DependencyInjection
{
    public sealed class RuntimeContainer : IResolver, IDisposable
    {
        private readonly Dictionary<(Type,string), List<Binding>> _bindings;
        private readonly List<IDisposable> _disposables = new();

        internal RuntimeContainer(
            Dictionary<(Type,string), List<Binding>> bindings)
        {
            _bindings = bindings;
        }

        public T Resolve<T>(string id = null)
            where T : class
        {
            var key = (typeof(T), id);

            if (!_bindings.TryGetValue(key, out var list) || list.Count == 0)
                throw new InvalidOperationException(
                    $"No binding found for {typeof(T).Name}");

            var instance = (T)list[0].Resolve(this);

            TrackDisposable(instance);
            return instance;
        }

        public T[] ResolveAll<T>() where T : class
        {
            var type = typeof(T);

            return _bindings
                .Where(kv => kv.Key.Item1 == type)
                .SelectMany(kv => kv.Value)
                .Select(b =>
                {
                    var obj = (T)b.Resolve(this);
                    TrackDisposable(obj);
                    return obj;
                })
                .ToArray();
        }

        private void TrackDisposable(object obj)
        {
            if (obj is IDisposable d && !_disposables.Contains(d))
                _disposables.Add(d);
        }

        public void Dispose()
        {
            for (int i = _disposables.Count - 1; i >= 0; i--)
                _disposables[i].Dispose();

            _disposables.Clear();
        }
    }
}
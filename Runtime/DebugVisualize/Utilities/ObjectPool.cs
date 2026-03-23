using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_DEBUG || DEVELOPMENT_BUILD
namespace Snm.Runtime.DebugVisualize
{
    public class ObjectPool<T> where T : class
    {
        private readonly Queue<T> _available = new();
        private readonly List<T> _active = new();
        private readonly Func<T> _factory;
        private readonly Action<T> _reset;
        private readonly int _maxSize;

        public ObjectPool(Func<T> factory, Action<T> reset = null, int maxSize = 100)
        {
            _factory = factory;
            _reset = reset;
            _maxSize = maxSize;
        }

        public int CountAvailable => _available.Count;
        public int CountActive => _active.Count;

        public T Get()
        {
            T item;
            if (_available.Count > 0)
            {
                item = _available.Dequeue();
            }
            else if (_active.Count < _maxSize)
            {
                item = _factory();
            }
            else
            {
                return null;
            }

            _active.Add(item);
            return item;
        }

        public void Return(T item)
        {
            if (item == null) return;
            
            _active.Remove(item);
            _reset?.Invoke(item);
            _available.Enqueue(item);
        }

        public void ReturnAll()
        {
            foreach (var item in _active)
            {
                _reset?.Invoke(item);
                _available.Enqueue(item);
            }
            _active.Clear();
        }

        public void Clear()
        {
            _available.Clear();
            _active.Clear();
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count && _available.Count + _active.Count < _maxSize; i++)
            {
                var item = _factory();
                _available.Enqueue(item);
            }
        }
    }

    public class LineRendererPool
    {
        private readonly ObjectPool<LineRenderer> _pool;
        private readonly Material _defaultMaterial;

        public LineRendererPool(Material defaultMaterial, int maxSize = 200)
        {
            _defaultMaterial = defaultMaterial;
            _pool = new ObjectPool<LineRenderer>(CreateNew, Reset, maxSize);
        }

        public int CountAvailable => _pool.CountAvailable;
        public int CountActive => _pool.CountActive;

        private LineRenderer CreateNew()
        {
            var go = new GameObject("DebugLine");
            go.hideFlags = HideFlags.HideAndDontSave;
            var lr = go.AddComponent<LineRenderer>();
            lr.material = _defaultMaterial;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        private void Reset(LineRenderer lr)
        {
            lr.gameObject.SetActive(false);
            lr.positionCount = 0;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.startColor = Color.white;
            lr.endColor = Color.white;
        }

        public LineRenderer Get()
        {
            var lr = _pool.Get();
            if (lr != null)
            {
                lr.gameObject.SetActive(true);
            }
            return lr;
        }

        public void Return(LineRenderer lr)
        {
            _pool.Return(lr);
        }

        public void ReturnAll()
        {
            _pool.ReturnAll();
        }

        public void Clear()
        {
            _pool.Clear();
        }

        public void Prewarm(int count)
        {
            _pool.Prewarm(count);
        }
    }
}
#endif

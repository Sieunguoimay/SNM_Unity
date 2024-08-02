using System.Collections.Generic;
using UnityEngine;

namespace GameNodeHierarchy
{
    public class ObjectPool<T> where T : Object
    {
        private readonly T prefab;
        private readonly Transform container;
        private readonly Queue<T> objects = new();
        private readonly HashSet<T> activeObjects = new();

        public T Prefab => prefab;
        public HashSet<T> ActiveObjects => activeObjects;

        public ObjectPool(T prefab, int initialSize, Transform container)
        {
            this.prefab = prefab;
            this.container = container;
            for (int i = 0; i < initialSize; i++)
            {
                T newObj = Object.Instantiate(prefab, container);
                SetObjectActive(newObj, false);
                objects.Enqueue(newObj);
            }
        }

        public virtual T Get()
        {
            if (objects.Count > 0)
            {
                var obj = objects.Dequeue();
                SetObjectActive(obj, true);
                activeObjects.Add(obj);
                return obj;
            }
            else
            {
                var newObj = Object.Instantiate(prefab, container);
                SetObjectActive(newObj, true);
                activeObjects.Add(newObj);
                return newObj;
            }
        }

        public virtual void ReturnToPool(T obj)
        {
            SetObjectActive(obj, false);
            activeObjects.Remove(obj);
            objects.Enqueue(obj);
        }

        private static void SetObjectActive(T newObj, bool active)
        {
            if (newObj is Component c)
            {
                c.gameObject.SetActive(active);
            }
            else if (newObj is GameObject go)
            {
                go.SetActive(active);
            }
        }
    }
}
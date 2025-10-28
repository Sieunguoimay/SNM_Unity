using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sieunguoimay.Tools
{
    public static class IndexableTypeHelper
    {
        public static bool TryGetElementType(Type type, out Type elementType)
        {
            elementType = typeof(object);

            if (type.IsArray)
            {
                elementType = type.GetElementType();
                return true;
            }
            if (TryGetGenericArg(type, typeof(IList<>), out var t))
            {
                elementType = t;
                return true;
            }
            if (TryGetGenericArg(type, typeof(IReadOnlyList<>), out t))
            {
                elementType = t;
                return true;
            }
            if (typeof(IList).IsAssignableFrom(type))
            {
                elementType = typeof(object);
                return true;
            }

            return false;
        }

        public static object GetElementAtIndex(object obj, int index)
        {
            if (obj is Array arr)
            {
                return (index >= 0 && index < arr.Length) ? arr.GetValue(index) : null;
            }
            if (obj is IList list)
            {
                return (index >= 0 && index < list.Count) ? list[index] : null;
            }

            var type = obj.GetType();

            // IReadOnlyList<T> / IList<T> via interface reflection
            var indexableIface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType
                    && (i.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
                        || i.GetGenericTypeDefinition() == typeof(IList<>)));

            if (indexableIface != null)
            {
                var countProp = indexableIface.GetProperty("Count");
                var indexer = indexableIface.GetProperty("Item");
                if (countProp != null && indexer != null)
                {
                    int count = (int)countProp.GetValue(obj);
                    if (index >= 0 && index < count)
                        return indexer.GetValue(obj, new object[] { index });
                    return null;
                }
            }
            return null;
        }

        public static int GetElementCount(object obj)
        {
            if (obj is Array arr)
            {
                return arr.Length;
            }
            if (obj is IList list)
            {
                return list.Count;
            }

            var type = obj.GetType();

            // IReadOnlyList<T> / IList<T> via interface reflection
            var indexableIface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType
                    && (i.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
                        || i.GetGenericTypeDefinition() == typeof(IList<>)));

            if (indexableIface != null)
            {
                var countProp = indexableIface.GetProperty("Count");
                var indexer = indexableIface.GetProperty("Item");
                if (countProp != null && indexer != null)
                {
                    return (int)countProp.GetValue(obj);
                }
            }

            return 0;
        }

        private static bool TryGetGenericArg(Type type, Type genericIface, out Type arg)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericIface)
            {
                arg = type.GetGenericArguments()[0];
                return true;
            }

            var match = type
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericIface);

            if (match != null)
            {
                arg = match.GetGenericArguments()[0];
                return true;
            }

            arg = null;
            return false;
        }
    }
}

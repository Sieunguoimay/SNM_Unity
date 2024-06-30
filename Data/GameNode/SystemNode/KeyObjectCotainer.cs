using System.Collections.Generic;

namespace GameNode
{
    public class KeyObjectCotainer : IKeyObjectCotainer
    {
        private readonly Dictionary<string, object> dic = new();

        public void AddObject<TData>(TData obj, string key)
        {
            dic.TryAdd(key, obj);
        }

        public TData GetObject<TData>(string key)
        {
            return dic.TryGetValue(key, out var o) ? (TData)o : default;
        }

        public void RemoveObject(string key)
        {
            dic.Remove(key);
        }
    }
}
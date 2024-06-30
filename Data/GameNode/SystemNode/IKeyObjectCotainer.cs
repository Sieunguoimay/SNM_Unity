namespace GameNode
{
    public interface IKeyObjectCotainer
    {
        TData GetObject<TData>(string key);
        void AddObject<TData>(TData obj, string key);
        void RemoveObject(string key);
    }
}
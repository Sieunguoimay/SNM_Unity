using System;
using System.Collections.Generic;

public interface IListObject
{
    IEnumerable<object> DynamicList { get; }
    event Action<IListObject> ListChangedEvent;
}

public class ListObject<TType> : IListObject where TType : class
{
    private readonly List<TType> list = new();

    public IReadOnlyList<TType> List => list;

    public IEnumerable<object> DynamicList
    {
        get
        {
            foreach (var i in list)
            {
                yield return i;
            }
        }
    }

    public event Action<ListObject<TType>, TType> AddedEvent;
    public event Action<ListObject<TType>, TType> RemovedEvent;
    public event Action<IListObject> ListChangedEvent;


    public void AddObject(TType obj)
    {
        list.Add(obj);
        AddedEvent?.Invoke(this, obj);
        ListChangedEvent?.Invoke(this);
    }

    public void RemoveObject(TType obj)
    {
        list.Remove(obj);
        RemovedEvent?.Invoke(this, obj);
        ListChangedEvent?.Invoke(this);
    }

    public void Clear()
    {
        list.Clear();
        ListChangedEvent?.Invoke(this);
    }
}

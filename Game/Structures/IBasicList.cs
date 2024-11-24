using System;
using System.Collections.Generic;

namespace SNM.Structures
{
    public interface IBasicList<T>
    {
        void Add(T item);
        void Remove(T item);
        IReadOnlyList<T> Items { get; }
        event Action<IBasicList<T>, T> OnItemAdded;
        event Action<IBasicList<T>, T> OnItemRemoved;
        event Action<IBasicList<T>> OnItemsChanged;
    }
}
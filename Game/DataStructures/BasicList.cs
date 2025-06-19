using System;
using System.Collections.Generic;

namespace Snm.DataStructures
{
    public class BasicList<T> : IBasicList<T>
    {
        private readonly List<T> items = new();
        IReadOnlyList<T> IBasicList<T>.Items => items;

        private Action<BasicList<T>, T> _onItemAdded;
        private Action<BasicList<T>, T> _onItemRemoved;
        private Action<BasicList<T>> _onItemsChanged;

        event Action<IBasicList<T>, T> IBasicList<T>.OnItemAdded
        {
            add { _onItemAdded += value; }
            remove { _onItemAdded -= value; }
        }
        event Action<IBasicList<T>, T> IBasicList<T>.OnItemRemoved
        {
            add { _onItemRemoved += value; }
            remove { _onItemRemoved -= value; }
        }
        event Action<IBasicList<T>> IBasicList<T>.OnItemsChanged
        {
            add { _onItemsChanged += value; }
            remove { _onItemsChanged -= value; }
        }

        void IBasicList<T>.Add(T item)
        {
            Add(item);
        }

        void IBasicList<T>.Remove(T item)
        {
            Remove(item);
        }

        private void Add(T item)
        {
            if (!items.Contains(item))
            {
                items.Add(item);
                _onItemAdded?.Invoke(this, item);
                _onItemsChanged?.Invoke(this);
            }
        }

        protected void Remove(T item)
        {
            if (items.Contains(item))
            {
                items.Remove(item);
                _onItemRemoved?.Invoke(this, item);
                _onItemsChanged?.Invoke(this);
            }
        }
    }
}
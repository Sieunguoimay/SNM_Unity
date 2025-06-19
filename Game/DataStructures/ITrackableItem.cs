using System.Collections.Generic;

namespace Snm.DataStructures
{
    public interface IListTrackingItem<TList> where TList : ITrackedList
    {
        void AddList(TList list);
        void RemoveList(TList list);
    }

    public interface ITrackableItem : IListTrackingItem<ITrackedList>
    {
        IReadOnlyList<ITrackedList> TrackedLists { get; }
    }
}
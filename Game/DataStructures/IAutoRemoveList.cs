namespace Snm.DataStructures
{
    public interface IAutoRemoveList : ITrackedList
    {
        void AutoRemove(IListTrackingItem<IAutoRemoveList> item);
    }
}
namespace SNM.DataStructures
{
    public class AutoRemoveList<T> :
        TrackedList<T, IAutoRemoveList>, IBasicList<T>,
        IAutoRemoveList, ITrackedList
    {
        void IAutoRemoveList.AutoRemove(IListTrackingItem<IAutoRemoveList> listTrackingItem)
        {
            if (listTrackingItem is T item)
            {
                Remove(item);
            }
        }
    }

}
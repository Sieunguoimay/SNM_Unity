namespace SNM.Structures
{
    public class TrackedList<T, TList> : 
        BasicList<T>, IBasicList<T>,
        ITrackedList
        where TList : ITrackedList
    {
        public TrackedList()
        {
            var basicList = (IBasicList<T>)this;
            basicList.OnItemAdded += BasicList_OnItemAdded;
            basicList.OnItemRemoved += BasicList_OnItemRemoved;
        }

        private void BasicList_OnItemAdded(IBasicList<T> list, T item)
        {
            if (item is IListTrackingItem<TList> listTrackingItem)
            {
                listTrackingItem.AddList((TList)(ITrackedList)this);
            }
        }

        private void BasicList_OnItemRemoved(IBasicList<T> list, T item)
        {
            if (item is IListTrackingItem<TList> listTrackingItem)
            {
                listTrackingItem.RemoveList((TList)(ITrackedList)this);
            }
        }
    }
}
namespace SNM.Structures
{
    public interface IAutoRemoveList : ITrackedList
    {
        void AutoRemove(IListTrackingItem<IAutoRemoveList> item);
    }
}
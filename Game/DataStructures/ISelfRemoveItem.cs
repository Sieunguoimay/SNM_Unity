namespace SNM.DataStructures
{
    public interface ISelfRemoveItem : IListTrackingItem<IAutoRemoveList>
    {
        void RemoveSelf();
    }
}
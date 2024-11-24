namespace SNM.Structures
{
    public interface ISelfRemoveItem : IListTrackingItem<IAutoRemoveList>
    {
        void RemoveSelf();
    }
}
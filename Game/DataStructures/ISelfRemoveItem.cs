namespace Snm.DataStructures
{
    public interface ISelfRemoveItem : IListTrackingItem<IAutoRemoveList>
    {
        void RemoveSelf();
    }
}
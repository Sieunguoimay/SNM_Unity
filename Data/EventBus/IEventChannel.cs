namespace EventBus
{
    public interface IEventChannel
    {
        void AddHandler(IEventHandler handler);
        void RemoveHandler(IEventHandler handler);
        void Dispatch(IEventObject eventObject, IEventSender sender, object data);
    }
}
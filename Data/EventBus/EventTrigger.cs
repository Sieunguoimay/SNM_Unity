namespace EventBus
{
    public interface IEventHandler
    {
        void OnReceivedEvent(IEventObject eventObject, IEventSender sender, object data);
    }

    public interface IEventSender
    {
    }
}
namespace EventBus
{
    public interface IEventReceiver{
        void OnReceiveEvent(IEvent evt);
    }
}
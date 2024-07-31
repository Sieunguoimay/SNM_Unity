namespace EventSystem
{
    public interface IEventReceiver
    {
        void OnReceiveEvent(IEvent evt);
    }
}
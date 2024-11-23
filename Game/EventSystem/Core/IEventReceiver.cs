namespace EventSystem
{
    public interface IEventReceiver
    {
        void OnReceiveEvent(EventObject evt, object data);
    }
}
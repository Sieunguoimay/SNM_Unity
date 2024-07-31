namespace EventSystem
{
    public class EventDispatcherMB_Empty : EventDispatcherMB<EmptyEvent>
    {
        protected override IEvent CreateEvent(EventInfo eventInfo)
        {
            return new EmptyEvent(eventInfo);
        }
    }
}
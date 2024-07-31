namespace EventSystem
{
    public interface IEvent
    {
        EventInfo EventInfo { get; }
    }

    public class EmptyEvent : IEvent
    {
        public EventInfo EventInfo {get;}

        public EmptyEvent(EventInfo eventInfo)
        {
            EventInfo = eventInfo;
        }
    }
}
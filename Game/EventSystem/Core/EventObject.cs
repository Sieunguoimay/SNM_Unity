namespace EventSystem
{
    public class EventObject
    {
        public string ID { get; }
        public string DisplayName { get; }
        public EventObject(string id, string eventName)
        {
            ID = id;
            DisplayName = eventName;
        }
    }
}
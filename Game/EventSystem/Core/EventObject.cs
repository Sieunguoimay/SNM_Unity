using Identification;

namespace EventSystem
{
    public class EventObject : IdentifiedObject
    {
        public string DisplayName { get; }
        public EventObject(ID id, string eventName) : base(id)
        {
            DisplayName = eventName;
        }
    }
}
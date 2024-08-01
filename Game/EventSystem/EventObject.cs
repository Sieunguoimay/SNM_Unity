using Identification;

namespace EventSystem
{
    public class EventObject : IdentifiedObject
    {
        public string DisplayName { get; }
        public EventObject(SUID suid, string eventName) : base(suid)
        {
            DisplayName = eventName;
        }
    }
}
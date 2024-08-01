namespace EventSystem
{
    public class EventInfo
    {
        public string DisplayName { get; }
        public string GUID { get; }

        public EventInfo(string guid,string displayName)
        {
            GUID = guid;
            DisplayName = displayName;
        }
    }
}
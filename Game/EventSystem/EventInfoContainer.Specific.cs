using System.Collections;
using System.Collections.Generic;

namespace EventSystem
{
    public partial class EventInfoContainer
    {
        public EventInfo GameStarted { get; } = new(GUIDs.GUID1, nameof(GameStarted));

        public IEnumerable<EventInfo> AllEventInfos
        {
            get
            {
                yield return GameStarted;
            }
        }
    }

    public static class GUIDs
    {
        public static string GUID1 = "ABCDX";
        public static string GUID2 = "ABCDX";
    }
}
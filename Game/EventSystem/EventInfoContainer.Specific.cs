using System.Collections;
using System.Collections.Generic;

namespace EventSystem
{
    public partial class EventInfoContainer
    {
        public EventInfo GameStarted { get; } = new(nameof(GameStarted));
        
        public IEnumerable<EventInfo> AllEventInfos
        {
            get
            {
                yield return GameStarted;
            }
        }
    }
}
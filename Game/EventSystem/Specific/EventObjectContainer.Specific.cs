using System.Collections.Generic;
using Snm.Identification;

namespace EventSystem
{
    public partial class EventObjectContainer
    {
        public EventObject OnGameStarted { get; } = new(EventSystemIDs._e9dc4420, nameof(OnGameStarted));

        public IEnumerable<EventObject> GetAllEventObjects()
        {
            yield return OnGameStarted;
        }
    }
}
using System.Collections.Generic;
using Identification;

namespace EventSystem
{
    public partial class EventObjectContainer
    {
        public EventObject OnGameStarted { get; } = new(null, nameof(OnGameStarted));

        public IEnumerable<EventObject> GetAllEventObjects()
        {
            yield return OnGameStarted;
        }
    }
}
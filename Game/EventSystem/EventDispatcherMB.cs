using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EventSystem
{
    public abstract class EventDispatcherMB<TEvent> : MonoBehaviour where TEvent : IEvent
    {
        [StringSelector(nameof(EventIDs))]
        [SerializeField] private string eventGUID;

        private EventInfo _eventInfo;

        private IEnumerable<string> EventIDs => EventInfoContainer.Instance.AllEventInfos.Select(e => e.GUID);

        private void Awake()
        {
            _eventInfo = EventInfoContainer.Instance.AllEventInfos
                .FirstOrDefault(i => i.GUID == eventGUID);

            if (_eventInfo == null)
            {
                Debug.LogError($"Event not found {eventGUID}");
            }
        }

        [ContextMenu("Dispatch")]
        public void Dispatch()
        {
            EventDispatcher.Instance.Dispatch(CreateEvent(_eventInfo));
        }

        protected abstract IEvent CreateEvent(EventInfo eventInfo);
    }
}
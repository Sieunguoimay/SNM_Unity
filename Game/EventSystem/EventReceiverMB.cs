using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace EventSystem
{
    public class EventReceiverMB : MonoBehaviour, IEventReceiver
    {
        [SerializeField] private UnityEvent onReceived;

        [StringSelector(nameof(EventIDs))]
        [SerializeField] private string eventID;

        private EventInfo _eventInfo;

        private IEnumerable<string> EventIDs => EventInfoContainer.Instance.AllEventInfos.Select(e => e.ID);

        private void Awake()
        {
            _eventInfo = EventInfoContainer.Instance.AllEventInfos
                .FirstOrDefault(i => i.ID == eventID);

            if (_eventInfo == null)
            {
                Debug.LogError($"Event not found {eventID}");
            }
        }

        private void OnEnable()
        {
            if (_eventInfo != null)
            {
                EventDispatcher.Instance.Register(_eventInfo, this);
            }
        }

        private void OnDisable()
        {
            if (_eventInfo != null)
            {
                EventDispatcher.Instance.Unregister(_eventInfo, this);
            }
        }

        public void OnReceiveEvent(IEvent evt)
        {
            onReceived?.Invoke();
        }
    }
}
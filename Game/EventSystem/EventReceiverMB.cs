using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace EventSystem
{
    public class EventReceiverMB : MonoBehaviour, IEventReceiver
    {
        [SerializeField] private UnityEvent onReceived;
        [SerializeField] private EventObjectSelector eventSelector;

        private void Awake()
        {
            eventSelector.Cache();
        }

        private void OnEnable()
        {
            if (eventSelector.EventObject != null)
            {
                EventDispatcher.Instance.Register(eventSelector.EventObject, this);
            }
        }

        private void OnDisable()
        {
            if (eventSelector.EventObject != null)
            {
                EventDispatcher.Instance.Unregister(eventSelector.EventObject, this);
            }
        }

        public void OnReceiveEvent(EventObject evt)
        {
            onReceived?.Invoke();
        }
    }
}
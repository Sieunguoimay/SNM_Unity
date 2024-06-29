using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace EventBus
{
    public class EventHandler_UnityEvent : MonoBehaviour, IEventHandler
    {
        [SerializeField] private EventSelector eventSelector;
        [SerializeField] private UnityEvent unityEvent;

        private void OnEnable()
        {
            eventSelector.Channel.AddHandler(this);
        }

        private void OnDisable()
        {
            eventSelector.Channel.RemoveHandler(this);
        }

        public void OnReceivedEvent(IEventObject eventObject, IEventSender trigger, object data)
        {
            if (eventSelector.Event.Interface == eventObject)
            {
                unityEvent?.Invoke();
            }
        }
    }
}
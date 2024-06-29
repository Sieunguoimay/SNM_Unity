using System;
using System.Collections.Generic;
using InspectorExtensions;
using UnityEngine;

namespace EventBus
{
    public class EventChannel : ScriptableObject
    {
        private readonly List<IEventHandler> handlers = new();

        [RevealNonSerialized] 
        public IReadOnlyList<IEventHandler> Handlers => handlers;
        
        public void AddHandler(IEventHandler handler)
        {
            handlers.Add(handler);
        }

        public void RemoveHandler(IEventHandler handler)
        {
            handlers.Remove(handler);
        }

        public void Dispatch(IEventObject eventObject, IEventSender sender, object data)
        {
            AssertData(eventObject, data);
            foreach (var eh in handlers)
            {
                eh.OnReceivedEvent(eventObject, sender, data);
            }
        }

        private void AssertData(IEventObject eventObject, object data)
        {
            if (!(eventObject.ConstraintDataType?.IsInstanceOfType(data) ?? true))
            {
                Debug.LogError($"Given data {data} is not supported by this Event {eventObject.EventName} which requires data type of {eventObject.ConstraintDataType.Name}");
            }
        }
    }
}
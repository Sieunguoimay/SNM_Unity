using System.Collections.Generic;
using UnityEngine;

namespace EventBus
{
    public class EventChannelMB : MonoBehaviour
    {
        private readonly List<IEventHandler> handlers = new();

        public IReadOnlyList<IEventHandler> Handlers => handlers;

        public void AddHandler(IEventHandler handler)
        {
            handlers.Add(handler);
        }

        public void RemoveHandler(IEventHandler handler)
        {
            handlers.Remove(handler);
        }

        public void Dispatch(IEventObject evenObject, IEventSender sender, object data)
        {
            foreach (var eh in handlers)
            {
                eh.OnReceivedEvent(evenObject, sender, data);
            }
        }
    }

}
using System.Collections.Generic;
using System.Linq;

namespace EventSystem
{
    public class EventDispatcher
    {
        private static EventDispatcher _instance;
        public static EventDispatcher Instance => _instance ??= new();

        private readonly Dictionary<string, HashSet<IEventReceiver>> dictionary = new();

        public void Register(EventInfo eventInfo, IEventReceiver receiver)
        {
            if (dictionary.TryGetValue(eventInfo.DisplayName, out var a))
            {
                a.Add(receiver);
            }
            else
            {
                dictionary.Add(eventInfo.DisplayName, new HashSet<IEventReceiver> { receiver });
            }
        }

        public void Unregister(EventInfo eventInfo, IEventReceiver receiver)
        {
            if (dictionary.TryGetValue(eventInfo.DisplayName, out var a))
            {
                a.Remove(receiver);
            }
        }

        public void Dispatch(IEvent evt)
        {
            if (dictionary.TryGetValue(evt.EventInfo.DisplayName, out var d))
            {
                foreach (var receiver in d)
                {
                    receiver.OnReceiveEvent(evt);
                }
            }
        }

        public IEnumerable<IEventReceiver> GetEventReceivers(EventInfo eventInfo)
        {
            if (dictionary.TryGetValue(eventInfo.DisplayName, out var rs))
            {
                return rs;
            }
            return Enumerable.Empty<IEventReceiver>();
        }
    }
}
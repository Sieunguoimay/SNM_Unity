using System.Collections.Generic;
using System.Linq;

namespace EventSystem
{
    public class EventDispatcher
    {
        private static EventDispatcher _instance;
        public static EventDispatcher Instance => _instance ??= new();

        private readonly Dictionary<string, HashSet<IEventReceiver>> dictionary = new();

        public void Register(EventObject eventInfo, IEventReceiver receiver)
        {
            if (dictionary.TryGetValue(eventInfo.ID, out var a))
            {
                a.Add(receiver);
            }
            else
            {
                dictionary.Add(eventInfo.ID, new HashSet<IEventReceiver> { receiver });
            }
        }

        public void Unregister(EventObject eventInfo, IEventReceiver receiver)
        {
            if (dictionary.TryGetValue(eventInfo.ID, out var a))
            {
                a.Remove(receiver);
            }
        }

        public void Dispatch(EventObject evt, object data)
        {
            if (dictionary.TryGetValue(evt.ID, out var d))
            {
                foreach (var receiver in d)
                {
                    receiver.OnReceiveEvent(evt, data);
                }
            }
        }

#if UNITY_EDITOR
        public IEnumerable<IEventReceiver> GetEventReceivers(EventObject eventObject)
        {
            if (dictionary.TryGetValue(eventObject.ID, out var rs))
            {
                return rs;
            }
            return Enumerable.Empty<IEventReceiver>();
        }
#endif
    }
}
using System.Collections.Generic;

namespace EventBus
{
    public class EventDispatcher{
        
        private static EventDispatcher _instance;
        public static EventDispatcher Instance=>_instance ??= new ();

        private readonly HashSet<IEventReceiver> receivers = new();

        public void Register(IEventReceiver receiver){
            receivers.Add(receiver);
        }

        public void Unregister(IEventReceiver receiver){
            receivers.Remove(receiver);
        }

        public void Dispatch(IEvent evt){
            foreach(var receiver in receivers){
                receiver.OnReceiveEvent(evt);
            }
        }
    }
}
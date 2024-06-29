using UnityEngine;

namespace EventBus
{
    public class EventTriggerMB : MonoBehaviour, IEventSender
    {
        [SerializeField] private EventSelector eventSelector;

        [ObjectSelector]
        [SerializeField] private Object triggerData;

        [ContextMenu("Trigger")]
        public void TriggerEvent() => eventSelector.Channel.Dispatch(eventSelector.Event, this, triggerData);
    }
}
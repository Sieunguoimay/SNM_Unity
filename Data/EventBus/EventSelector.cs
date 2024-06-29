using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EventBus
{
    [System.Serializable]
    public class EventSelector
    {
        [SerializeField] private EventChannel channel;
        [SubAssetSelect(nameof(channel))]
        [SerializeField] private EventObject eventObject;

        public EventObject Event => eventObject;
        public EventChannel Channel => channel;
    }
}
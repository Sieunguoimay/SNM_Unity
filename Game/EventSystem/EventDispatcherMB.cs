using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EventSystem
{
    public abstract class EventDispatcherMB : MonoBehaviour
    {
        [SerializeField] private EventObjectSelector eventSelector;

        [ContextMenu("Dispatch")]
        public void Dispatch()
        {
            EventDispatcher.Instance.Dispatch(eventSelector.EventObject);
        }
    }
}
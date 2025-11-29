using System;
using UnityEngine;
using UnityEngine.Events;

namespace Snm.Visual.Animation
{
    public class AnimationEventDispatcher : MonoBehaviour
    {
        [SerializeField] private UnityEventString onStringEvent;

        public event Action<AnimationEventDispatcher, string> StringEventDispatched;

        public void DispatchStringEvent(string str)
        {
            onStringEvent?.Invoke(str);
            StringEventDispatched?.Invoke(this, str);
        }

        [Serializable] private class UnityEventString : UnityEvent<string> { }
    }
}

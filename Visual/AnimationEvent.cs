using System;
using EventBus;
using UnityEngine;
using UnityEngine.Events;

public class AnimationEvent : MonoBehaviour
{
    [SerializeField] private UnityEventString onEvent;

    public void OnStringEvent(string eventName)
    {
        onEvent?.Invoke(eventName);
    }

    [Serializable] private class UnityEventString : UnityEvent<string> { }
}

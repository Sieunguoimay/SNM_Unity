using System;
using UnityEngine;
using UnityEngine.Events;

public class ComponentLifeCycleEvents : MonoBehaviour
{
    [SerializeField] private UnityEvent onStart;
    [SerializeField] private UnityEvent onEnable;
    public event Action<ComponentLifeCycleEvents> EnabledEvent;
    public event Action<ComponentLifeCycleEvents> DisabledEvent;
    public event Action<ComponentLifeCycleEvents> StartedEvent;
    public event Action<ComponentLifeCycleEvents> DestroyedEvent;

    private void OnEnable()
    {
        EnabledEvent?.Invoke(this);
        onEnable?.Invoke();
    }

    private void OnDisable()
    {
        DisabledEvent?.Invoke(this);
    }

    private void Start()
    {
        StartedEvent?.Invoke(this);
        onStart?.Invoke();
    }

    private void OnDestroy()
    {
        DestroyedEvent?.Invoke(this);
    }
}

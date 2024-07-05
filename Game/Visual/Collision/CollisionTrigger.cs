using System;
using UnityEngine;
using UnityEngine.Events;

public class CollisionTrigger : MonoBehaviour
{
    [ObjectSelector(typeof(ICollisionEventHandler))]
    [SerializeField] private UnityEngine.Object handler;
    [SerializeField] private UnityEvent onCollisionEnter;
    [SerializeField] private UnityEvent onCollisionExit;

    private ICollisionEventHandler _handler;

    public event Action<CollisionTrigger> TriggerEnterEvent;
    public event Action<CollisionTrigger> TriggerExitEvent;

    private void Awake()
    {
        _handler = handler as ICollisionEventHandler;
        if (_handler == null)
        {
            Debug.LogError("Assigned object has not implemented ICollisionEventHandler");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        _handler.OnCollisionEnter(this, other);
        onCollisionEnter?.Invoke();
        TriggerEnterEvent?.Invoke(this);
    }

    private void OnTriggerExit(Collider other)
    {
        _handler.OnCollisionExit(this, other);
        onCollisionExit?.Invoke();
        TriggerExitEvent?.Invoke(this);
    }
}

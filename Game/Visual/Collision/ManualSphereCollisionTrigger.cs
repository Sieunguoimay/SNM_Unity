using System;
using UnityEngine;
using UnityEngine.Events;

public class ManualSphereCollisionTrigger : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float radius = 1f;

    [SerializeField] private UnityEvent onTriggerEnter;
    [SerializeField] private UnityEvent onTriggerExit;

    private bool _isInside;

    public event Action<ManualSphereCollisionTrigger> TriggerEnterEvent;
    public event Action<ManualSphereCollisionTrigger> TriggerExitEvent;

    private void Start()
    {
        _isInside = Vector3.SqrMagnitude(target.position - transform.position) > radius * radius;
    }

    private void Update()
    {
        if (Vector3.SqrMagnitude(target.position - transform.position) > radius * radius)
        {
            if (!_isInside)
            {
                _isInside = true;
                onTriggerEnter?.Invoke();
                TriggerEnterEvent?.Invoke(this);
            }
        }
        else
        {
            if (_isInside)
            {
                _isInside = false;
                onTriggerExit?.Invoke();
                TriggerExitEvent?.Invoke(this);
            }
        }
    }
}
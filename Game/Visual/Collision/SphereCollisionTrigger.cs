using UnityEngine;
using UnityEngine.Events;

public class SphereCollisionTrigger : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float radius = 1f;
    [SerializeField] private UnityEvent onTriggerEnter;
    [SerializeField] private UnityEvent onTriggerExit;
    private bool _isInside;
    
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
            }
        }
        else
        {
            if (_isInside)
            {
                _isInside = false;
                onTriggerEnter?.Invoke();
            }
        }
    }
}
using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Use GameCollision instead
/// </summary>
[Obsolete]
public class CollisionDetector : MonoBehaviour
{
    [SerializeField] private UnityEvent onCollision;

    public event System.Action<CollisionDetector, GameObject> CollidedEvent;

    private void Start()
    {
#if UNITY_EDITOR
        if (GetComponent<Collider>() == null)
        {
            Debug.LogError("CollisionDetector: Not setup properly! Missing Collider Component");
        }
#endif
    }

    private void OnTriggerEnter(Collider other)
    {
        InvokeEvents(other.gameObject);
    }

    private void InvokeEvents(GameObject target)
    {
        CollidedEvent?.Invoke(this, target);
        onCollision?.Invoke();
    }
}

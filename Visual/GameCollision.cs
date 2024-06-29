using UnityEngine;
using UnityEngine.Events;

public class CollisionEventForwarder : MonoBehaviour
{
    [ObjectSelector]
    [SerializeField] private Object handler;
    [SerializeField] private UnityEvent onCollision;

    private ICollisionEventHandler _handler;

    private void Start()
    {
        _handler = handler as ICollisionEventHandler;
        if (_handler == null)
        {
            Debug.LogError("Assigned object has not implemented ICollisionEventHandler");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        _handler.OnCollision(this, other);
        onCollision?.Invoke();
    }
}

public interface ICollisionEventHandler
{
    void OnCollision(CollisionEventForwarder forwarder, Collider other);
}

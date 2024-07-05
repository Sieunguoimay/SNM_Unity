using UnityEngine;

public interface ICollisionEventHandler
{
    void OnCollisionEnter(CollisionTrigger forwarder, Collider other);
    void OnCollisionExit(CollisionTrigger forwarder, Collider other);
}

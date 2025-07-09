using UnityEngine;
using UnityEngine.Serialization;

namespace Snm.Visual.PhysicsAnimation
{
    public class ArrivalForceEvaluator : ForceEvaluator
    {
        [SerializeField] private float arrivalRadius = 1f;
        [FormerlySerializedAs("speed")]
        [SerializeField] private float steeringSpeed = 1f;
        [Tooltip("Steering behaviour instead of attraction. If you want attraction outsideRadius use PointForce please")]
        [SerializeField] private bool steerOutsideRadius = false;

        public override Vector3 Evaluate(IForceAnimation anim)
        {
            var offset = transform.position - anim.Target.position;
            var distance = offset.magnitude;

            Vector3 desiredVelocity;

            if (distance > arrivalRadius)
            {
                if (steerOutsideRadius)
                {
                    var velocityDir = offset / distance;
                    desiredVelocity = velocityDir * steeringSpeed;
                }
                else
                {
                    return Vector3.zero;
                }
            }
            else
            {
                desiredVelocity = offset * steeringSpeed / arrivalRadius;
            }

            var velOffset = desiredVelocity - anim.Velocity;

            return velOffset;
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, arrivalRadius);
        }
    }
}
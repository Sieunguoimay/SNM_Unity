using UnityEngine;

namespace Snm.Visual.PhysicsAnimation
{
    public class DirectionalForceEvaluator : ForceEvaluator
    {
        [SerializeField] private float force;

        public override Vector3 Evaluate(IForceAnimation anim)
        {
            return force * transform.forward;
        }
    }
}
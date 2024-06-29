using UnityEngine;

namespace ForceAnim
{
    public class PointForceEvaluator : ForceEvaluator
    {
        [SerializeField] private float force;
        [SerializeField] private Direction direction;

        public override Vector3 Evaluate(IForceAnimation anim)
        {
            var offset = anim.Target.position - transform.position;
            if (offset.sqrMagnitude > .01f)
            {
                var dir = Vector3.Normalize(offset);
                if (direction == Direction.Inward)
                {
                    dir = -dir;
                }
                return force * dir;
            }
            return Vector3.zero;
        }

        private enum Direction
        {
            Inward,
            Outward
        }
    }
}
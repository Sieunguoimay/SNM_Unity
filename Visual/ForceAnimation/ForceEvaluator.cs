using UnityEngine;

namespace ForceAnim
{
    public abstract class ForceEvaluator : MonoBehaviour
    {
        [SerializeField] protected AnimationCurve forceFactor;
        [SerializeField] protected float forceFactorMult = 1f;
        public Vector3 DoEvaluate(IForceAnimation anim)
        {
            return Evaluate(anim) * forceFactor.Evaluate(Mathf.Clamp01(anim.Time / anim.SimulationDuration)) * forceFactorMult;
        }
        public abstract Vector3 Evaluate(IForceAnimation anim);
    }
}
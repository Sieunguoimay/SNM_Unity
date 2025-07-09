using UnityEngine;

namespace Snm.Components
{
    public class TransformValueSetter : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private TargetType targetType;
        [SerializeField] private Axis axis;

        public void SetValue(float value)
        {
            var vector = GetVector3();
            SetAxis(ref vector, value);
            SetVector3(vector);
        }

        private Vector3 GetVector3()
        {
            return GetTransformProperty(targetType, target);
        }

        private static Vector3 GetTransformProperty(TargetType targetType, Transform target)
        {
            return targetType switch
            {
                TargetType.Position => target.position,
                TargetType.LocalPosition => target.localPosition,
                TargetType.LocalScale => target.localScale,
                TargetType.EulerAngles => target.eulerAngles,
                TargetType.LocalEulerAngles => target.localEulerAngles,
                _ => throw new System.NotImplementedException(),
            };
        }

        private void SetVector3(Vector3 vector)
        {
            SetTransformProperty(targetType, target, vector);
        }

        private static void SetTransformProperty(TargetType targetType, Transform target, Vector3 vector)
        {
            switch (targetType)
            {
                case TargetType.Position: target.position = vector; break;
                case TargetType.LocalPosition: target.localPosition = vector; break;
                case TargetType.EulerAngles: target.eulerAngles = vector; break;
                case TargetType.LocalEulerAngles: target.localEulerAngles = vector; break;
                case TargetType.LocalScale: target.localScale = vector; break;
            }
        }

        private void SetAxis(ref Vector3 vector, float value)
        {
            switch (axis)
            {
                case Axis.X: vector.x = value; break;
                case Axis.Y: vector.y = value; break;
                case Axis.Z: vector.z = value; break;
                case Axis.All: vector = Vector3.one * value; break;
            }
            ;
        }

        private enum Axis
        {
            X,
            Y,
            Z,
            All
        }

        private enum TargetType
        {
            Position,
            LocalPosition,
            LocalScale,
            EulerAngles,
            LocalEulerAngles,
        }

        private enum TargetScope
        {
            Local,
            Global
        }
    }
}
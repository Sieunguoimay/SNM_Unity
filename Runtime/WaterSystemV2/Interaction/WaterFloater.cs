using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// Drop-in floating: add to any rigidbody with a collider and water
    /// pushes it up. No wiring needed — registers itself globally. Volume is
    /// analytic for sphere/box/capsule colliders, bounds-box otherwise
    /// (implement <see cref="IWaterFloater"/> yourself for exact volumes).
    /// </summary>
    [AddComponentMenu("Snm/Water System V2/Water Floater")]
    public class WaterFloater : MonoBehaviour, IWaterFloater
    {
        [Tooltip("Leave empty to use the collider on this object.")]
        [SerializeField] private Collider sourceCollider;

        private Rigidbody _rigidbody;
        private float _cachedVolume = -1f;
        private Vector3 _cachedScale;

        public Rigidbody Rigidbody => _rigidbody;

        public Vector3 WorldPosition
            => sourceCollider != null ? sourceCollider.bounds.center : transform.position;

        public float GetTotalVolume()
        {
            if (sourceCollider == null) return 0f;

            var scale = sourceCollider.transform.lossyScale;
            if (_cachedVolume < 0f || scale != _cachedScale)
            {
                _cachedVolume = ComputeVolume(sourceCollider);
                _cachedScale = scale;
            }
            return _cachedVolume;
        }

        public float GetSubmergedVolume(float waterY)
        {
            if (sourceCollider == null) return 0f;

            float total = GetTotalVolume();
            var b = sourceCollider.bounds;

            if (waterY <= b.min.y) return 0f;
            if (waterY >= b.max.y) return total;

            if (sourceCollider is SphereCollider)
            {
                // Spherical cap: V = πh²(3r − h)/3.
                float r = b.extents.y;
                float h = Mathf.Clamp(waterY - b.min.y, 0f, 2f * r);
                float cap = Mathf.PI * h * h * (3f * r - h) / 3f;
                float full = 4f / 3f * Mathf.PI * r * r * r;
                return full > 0f ? total * (cap / full) : 0f;
            }

            // Box/capsule/other: linear slab fraction of the bounds height.
            float fraction = Mathf.Clamp01((waterY - b.min.y) / b.size.y);
            return total * fraction;
        }

        private static float ComputeVolume(Collider collider)
        {
            var s = collider.transform.lossyScale;

            switch (collider)
            {
                case SphereCollider sphere:
                {
                    float r = sphere.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                    return 4f / 3f * Mathf.PI * r * r * r;
                }
                case BoxCollider box:
                {
                    var size = Vector3.Scale(box.size, s);
                    return Mathf.Abs(size.x * size.y * size.z);
                }
                case CapsuleCollider capsule:
                {
                    // Cylinder + two hemisphere caps, axis-aware scaling.
                    float radiusScale = capsule.direction switch
                    {
                        0 => Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z)),
                        1 => Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z)),
                        _ => Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y)),
                    };
                    float heightScale = capsule.direction switch
                    {
                        0 => Mathf.Abs(s.x),
                        1 => Mathf.Abs(s.y),
                        _ => Mathf.Abs(s.z),
                    };

                    float r = capsule.radius * radiusScale;
                    float cylinderHeight = Mathf.Max(0f, capsule.height * heightScale - 2f * r);
                    return Mathf.PI * r * r * cylinderHeight + 4f / 3f * Mathf.PI * r * r * r;
                }
                default:
                {
                    var size = collider.bounds.size;
                    return size.x * size.y * size.z;
                }
            }
        }

        private void Awake()
        {
            if (sourceCollider == null) sourceCollider = GetComponentInChildren<Collider>();
            _rigidbody = sourceCollider != null
                ? sourceCollider.attachedRigidbody
                : GetComponentInParent<Rigidbody>();
        }

        private void OnEnable() => WaterInteraction.Register(this);
        private void OnDisable() => WaterInteraction.Unregister(this);
    }
}

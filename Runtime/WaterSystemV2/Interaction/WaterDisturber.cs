using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// Drop-in ripple maker: add to any object with a collider and it
    /// splashes on entry and leaves a wake while moving through water. No
    /// wiring needed — registers itself globally; every WaterBody picks it up.
    /// Contact shape is approximated as an ellipsoid fitted to the collider
    /// bounds (implement <see cref="IWaterDisturber"/> yourself for exact
    /// shapes).
    /// </summary>
    [AddComponentMenu("Snm/Water System V2/Water Disturber")]
    public class WaterDisturber : MonoBehaviour, IWaterDisturber
    {
        [Tooltip("Leave empty to use the collider on this object.")]
        [SerializeField] private Collider sourceCollider;

        private Rigidbody _rigidbody;
        private Vector3 _lastPosition;
        private Vector3 _fallbackVelocity;

        public Vector3 WorldPosition
        {
            get
            {
                var b = Bounds;
                return new Vector3(b.center.x, b.min.y, b.center.z);
            }
        }

        public Vector3 WorldVelocity
            => _rigidbody != null ? _rigidbody.linearVelocity : _fallbackVelocity;

        private Bounds Bounds
            => sourceCollider != null ? sourceCollider.bounds : new Bounds(transform.position, Vector3.one);

        public bool IsTouchingSurface(float surfaceY)
        {
            var b = Bounds;
            return b.min.y <= surfaceY && surfaceY <= b.max.y;
        }

        public float GetContactRadius(float surfaceY)
        {
            var b = Bounds;
            if (b.min.y > surfaceY || surfaceY > b.max.y) return 0f;

            float horizontal = Mathf.Max(b.extents.x, b.extents.z);
            if (b.extents.y <= 0f) return horizontal;

            // Ellipsoid slice: full radius at the middle, tapering to 0 at
            // the vertical extremes.
            float dy = Mathf.Clamp((surfaceY - b.center.y) / b.extents.y, -1f, 1f);
            return horizontal * Mathf.Sqrt(1f - dy * dy);
        }

        private void Awake()
        {
            if (sourceCollider == null) sourceCollider = GetComponentInChildren<Collider>();
            _rigidbody = sourceCollider != null
                ? sourceCollider.attachedRigidbody
                : GetComponentInParent<Rigidbody>();
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;
            WaterInteraction.Register(this);
        }

        private void OnDisable() => WaterInteraction.Unregister(this);

        private void Update()
        {
            if (_rigidbody != null) return;

            // No rigidbody — estimate velocity from movement.
            float dt = Time.deltaTime;
            if (dt > 0f)
                _fallbackVelocity = (transform.position - _lastPosition) / dt;
            _lastPosition = transform.position;
        }
    }
}

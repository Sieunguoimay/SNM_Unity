using UnityEngine;

namespace Snm.SpringMotion
{
    /// <summary>
    /// Tracks object movement and drives a damped spring simulation whose displacement
    /// is sent to the shader each frame. Vertices far from the pivot lag behind on
    /// sudden stops/direction changes, then spring back to rest.
    /// </summary>
    public class SpringMotionDriver : MonoBehaviour
    {
        [Header("Spring")]
        [Tooltip("Spring stiffness — higher values snap back faster.")]
        [SerializeField] float _stiffness = 40f;

        [Tooltip("Damping — higher values reduce oscillation faster.")]
        [SerializeField] float _damping = 4f;

        [Header("Vertex Falloff")]
        [Tooltip("Object-space distance used to normalize the falloff (should roughly match the mesh extent from pivot).")]
        [SerializeField] float _maxDistance = 1f;

        [Tooltip("Falloff exponent. 1 = linear, 2 = quadratic (tip moves more, base stays stiff).")]
        [SerializeField] float _falloffPower = 1.5f;

        [Tooltip("Clamps the maximum spring displacement (world units) to prevent extreme stretching.")]
        [SerializeField] float _maxDisplacement = 0.5f;

        [Header("Pivot")]
        [Tooltip("Attachment point in object space. Vertices at this point don't move.")]
        [SerializeField] Vector3 _pivotOffset = Vector3.zero;

        [Tooltip("Gizmo sphere radius for the pivot point visualization.")]
        [SerializeField] float _pivotGizmoRadius = 0.05f;

        [Header("Target")]
        [Tooltip("Renderer whose material receives the spring data. Auto-detected if left empty.")]
        [SerializeField] Renderer _renderer;

        // Spring state
        Vector3 _springDisplacement;
        Vector3 _springVelocity;

        // Motion tracking — uses two snapshots to handle both Update and FixedUpdate movers.
        // Position is sampled in both FixedUpdate and LateUpdate. The delta is computed from
        // whichever sample is freshest, so objects driven by Rigidbody (FixedUpdate) and
        // objects driven by Transform (Update) are both handled correctly.
        Vector3 _prevPosition;
        Vector3 _prevVelocity;
        Vector3 _fixedPosition;
        bool _fixedPositionDirty;

        // Shader property IDs
        static readonly int ID_SpringDisplacement = Shader.PropertyToID("_SpringDisplacement");
        static readonly int ID_SpringPivotOS = Shader.PropertyToID("_SpringPivotOS");
        static readonly int ID_SpringMaxDistance = Shader.PropertyToID("_SpringMaxDistance");
        static readonly int ID_SpringFalloffPower = Shader.PropertyToID("_SpringFalloffPower");

        MaterialPropertyBlock _mpb;

        void Awake()
        {
            if (_renderer == null)
                _renderer = GetComponent<Renderer>();

            _mpb = new MaterialPropertyBlock();
            _prevPosition = transform.position;
            _fixedPosition = _prevPosition;
        }

        void FixedUpdate()
        {
            // Capture position after physics step so Rigidbody-driven motion is tracked.
            _fixedPosition = transform.position;
            _fixedPositionDirty = true;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt < 1e-6f) return;

            // --- Track object motion ---
            // Use the FixedUpdate snapshot if it was written this frame (physics-driven object),
            // otherwise use the current transform position (script/animation-driven object).
            Vector3 currentPos;
            if (_fixedPositionDirty)
            {
                currentPos = _fixedPosition;
                _fixedPositionDirty = false;
            }
            else
            {
                currentPos = transform.position;
            }

            Vector3 velocity = (currentPos - _prevPosition) / dt;
            Vector3 acceleration = (velocity - _prevVelocity) / dt;

            _prevPosition = currentPos;
            _prevVelocity = velocity;

            // --- Damped spring simulation ---
            // The spring is driven by the object's acceleration (inertia effect).
            // displacement represents how much the "soft" part lags behind.
            Vector3 springForce = -_stiffness * _springDisplacement
                                  - _damping * _springVelocity;

            // Inertia: acceleration pushes the spring in the opposite direction
            _springVelocity += (springForce - acceleration) * dt;
            _springDisplacement += _springVelocity * dt;

            // Clamp to prevent extreme deformation
            float mag = _springDisplacement.magnitude;
            if (mag > _maxDisplacement)
                _springDisplacement = _springDisplacement / mag * _maxDisplacement;

            // --- Convert displacement to object space ---
            // The shader works in object space, so we transform the world-space
            // displacement into the object's local orientation.
            Vector3 localDisplacement = transform.InverseTransformDirection(_springDisplacement);

            // --- Push to shader via MaterialPropertyBlock (instancing-friendly) ---
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetVector(ID_SpringDisplacement, localDisplacement);
            _mpb.SetVector(ID_SpringPivotOS, _pivotOffset);
            _mpb.SetFloat(ID_SpringMaxDistance, _maxDistance);
            _mpb.SetFloat(ID_SpringFalloffPower, _falloffPower);
            _renderer.SetPropertyBlock(_mpb);
        }

        /// <summary>
        /// Apply an instantaneous impulse to the spring (e.g., on impact or pickup).
        /// Direction is in world space.
        /// </summary>
        public void AddImpulse(Vector3 worldImpulse)
        {
            _springVelocity += worldImpulse;
        }

        /// <summary>
        /// Immediately reset the spring to rest (no displacement, no velocity).
        /// </summary>
        public void ResetSpring()
        {
            _springDisplacement = Vector3.zero;
            _springVelocity = Vector3.zero;
            _prevVelocity = Vector3.zero;
        }

        void OnDisable()
        {
            // Clear displacement when disabled so mesh returns to original shape
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_mpb);
                _mpb.SetVector(ID_SpringDisplacement, Vector4.zero);
                _renderer.SetPropertyBlock(_mpb);
            }
        }

        void OnDrawGizmosSelected()
        {
            // Draw pivot point in world space
            Vector3 pivotWorld = transform.TransformPoint(_pivotOffset);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pivotWorld, _pivotGizmoRadius);

            // Draw a line from pivot to the current displaced tip direction
            if (Application.isPlaying && _springDisplacement.sqrMagnitude > 1e-6f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pivotWorld, pivotWorld + _springDisplacement);
            }

            // Draw maxDistance sphere to show the falloff range
            Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
            Gizmos.DrawWireSphere(pivotWorld, _maxDistance);
        }
    }
}

using UnityEngine;
using Snm.Runtime.DebugVisualize;

namespace Snm.Runtime.DebugVisualize
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsDebugVisualizerMB : MonoBehaviour
    {
        [SerializeField] private bool showSpeed = true;
        [SerializeField] private bool showMass = true;

        private Rigidbody _rigidbody;
        private DebugStatEntry _speedStat;
        private DebugStatEntry _massStat;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            if (showMass)
            {
                _massStat = DebugVisualize.ShowStat("mass", _rigidbody.mass, transform);
            }
        }

        private void OnDisable()
        {
            _speedStat = null;
            _massStat = null;
        }

        private void Update()
        {
            if (!DebugVisualize.Enabled) return;

            float speed = _rigidbody.linearVelocity.magnitude;
            if (showSpeed)
            {
                if (_speedStat == null)
                {
                    _speedStat = DebugVisualize.ShowStat("speed", () => _rigidbody.linearVelocity.magnitude, null, showBar: false, autoUpdate: true, target: transform);
                }
            }
        }
    }
}

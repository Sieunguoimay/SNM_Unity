using UnityEngine;

namespace Snm.Runtime.DebugDraw
{
    /// <summary>
    /// Attaches to any Rigidbody to show live mass and speed labels above it.
    /// Demonstrates the DebugDraw.Panel API.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PhysicsDebugVisualizerMB : MonoBehaviour
    {
        [SerializeField] private bool showMass  = true;
        [SerializeField] private bool showSpeed = true;

        private Rigidbody _rb;
        private StatPanel _panel;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        private void OnEnable()
        {
            _panel = DebugDraw.Panel(transform, Vector3.up * 2f);
            if (showMass)  _panel.Add("mass",  () => _rb.mass,                     autoUpdate: false);
            if (showSpeed) _panel.Add("speed", () => _rb.linearVelocity.magnitude,  autoUpdate: true);
        }

        private void OnDisable()
        {
            _panel?.Dispose();
            _panel = null;
        }
    }
}

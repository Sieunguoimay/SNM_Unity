using System.Collections.Generic;
using UnityEngine;

namespace Snm.GrassSystemV2
{
    /// <summary>
    /// Drop this on anything that should flatten grass while moving through it
    /// (player, enemies, projectiles, rolling props). Self-registering — no
    /// wiring, no interfaces to implement. <see cref="GrassWorld"/> reads all
    /// active disturbers each frame and stamps the bend canvas.
    /// </summary>
    [AddComponentMenu("Snm/Grass System V2/Grass Disturber")]
    public class GrassDisturber : MonoBehaviour
    {
        static readonly List<GrassDisturber> Active = new();

        /// <summary>All enabled disturbers. Read by GrassWorld once per frame.</summary>
        public static IReadOnlyList<GrassDisturber> ActiveDisturbers => Active;

        [Tooltip("Radius (meters) of flattened grass around this object.")]
        public float radius = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("How hard the grass is pushed down. 1 = flat on the ground.")]
        public float strength = 1f;

        [Tooltip("Max height above the grass root plane at which this still disturbs. Lets jumping objects clear the grass.")]
        public float maxHeightAboveGrass = 1.5f;

        Vector3 _previousPosition;
        Vector3 _lastMoveDirection = Vector3.forward;

        /// <summary>Direction of travel, kept from the last actual movement.</summary>
        public Vector3 MoveDirection => _lastMoveDirection;

        void OnEnable()
        {
            Active.Add(this);
            _previousPosition = transform.position;
        }

        void OnDisable()
        {
            Active.Remove(this);
        }

        /// <summary>Called by GrassWorld each frame. Updates the tracked move direction.</summary>
        public void TrackMovement()
        {
            var position = transform.position;
            var movement = position - _previousPosition;
            movement.y = 0f;
            if (movement.sqrMagnitude > 0.0001f)
            {
                _lastMoveDirection = movement.normalized;
                _previousPosition = position;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.3f, 0.6f);
            var position = transform.position;
            // Flattened circle on the XZ plane at the object's feet.
            const int segments = 24;
            var prev = position + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                var next = position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}

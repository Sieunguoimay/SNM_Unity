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
    [ExecuteAlways] // registers in edit mode too — dragging objects in the Scene view tramples grass
    [AddComponentMenu("Snm/Grass System V2/Grass Disturber")]
    public class GrassDisturber : MonoBehaviour
    {
        static readonly List<GrassDisturber> Active = new();

        /// <summary>All enabled disturbers. Read by GrassWorld once per frame.</summary>
        public static IReadOnlyList<GrassDisturber> ActiveDisturbers => Active;

        [Tooltip("Sphere radius in meters. Horizontally: grass past this is untouched (bend fades to zero, GREEN debug ring). Vertically: the disturber is treated as a sphere of this radius, so it stops flattening grass once its bottom lifts above the blade tops (jumping clears the grass automatically — no manual height field).")]
        public float outerRadius = 0.5f;

        [Tooltip("Inner core radius in meters, independent of Outer Radius. Grass within this is pressed fully flat; between here and the outer radius the bend fades out. Shown as the ORANGE debug ring. Clamped to Outer Radius.")]
        public float fullFlattenRadius = 0.25f;

        [Range(0f, 1f)]
        [Tooltip("How hard the grass is pushed down. 1 = flat on the ground.")]
        public float strength = 1f;

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

        // Direction only updates after real displacement — normalizing centimeter
        // jitter (physics, idle sway) would swing the push direction wildly for
        // blades right under the disturber.
        const float MinMoveForDirection = 0.05f;

        /// <summary>Called by GrassWorld each frame. Updates the tracked move direction.</summary>
        public void TrackMovement()
        {
            var position = transform.position;
            var movement = position - _previousPosition;
            movement.y = 0f;
            if (movement.sqrMagnitude > MinMoveForDirection * MinMoveForDirection)
            {
                _lastMoveDirection = movement.normalized;
                _previousPosition = position;
            }
        }

        void OnDrawGizmosSelected()
        {
            // Radius + flat-core circles + travel direction, shared with the
            // debug overlay so selection and overlay always look the same.
            GrassDebugOverlay.DrawDisturberGizmo(this);
        }
    }
}

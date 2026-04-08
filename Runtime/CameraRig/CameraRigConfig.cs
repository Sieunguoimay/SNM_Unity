using System;
using UnityEngine;

namespace Snm.CameraRig
{
    public enum ScreenConstraintMode
    {
        None,
        FixedWidth,       // Orthographic: maintains a fixed world-width
        VisibleArea,      // Perspective: adjusts FOV to keep a world-space rect visible
    }

    [Serializable]
    public class CameraRigConfig
    {
        public bool debugVisualize;

        [Header("Smoothing")]
        [Range(0.01f, 1f)] public float convergenceRateXY = 0.5f;
        [Range(0.01f, 1f)] public float convergenceRateZ = 0.5f;
        [Range(0.01f, 1f)] public float convergenceRateRotation = 0.5f;

        [Header("Dead Zone")]
        [Range(0f, 0.5f)] public float deadZoneX = 0f;
        [Range(0f, 0.5f)] public float deadZoneY = 0f;

        [Header("Padding")]
        [Range(0f, 0.5f)] public float ndcPaddingX = 0.05f;
        [Range(0f, 0.5f)] public float ndcPaddingY = 0.05f;

        [Header("Zoom Limits")]
        public float minDistance = 2f;
        public float maxDistance = 50f;

        [Header("Manual Zoom")]
        public bool enableManualZoom;
        [Range(0.01f, 1f)] public float zoomSmoothRate = 0.3f;
        public float zoomSensitivity = 2f;

        [Header("Look-Ahead")]
        [Range(0f, 2f)] public float lookAheadFactor = 0.3f;
        [Range(0f, 1f)] public float lookAheadMaxOffset = 0.3f;

        [Header("Pitch")]
        [Range(10f, 89f)] public float defaultPitch = 50f;
        [Range(0.01f, 1f)] public float pitchConvergenceRate = 0.2f;

        [Header("Camera Bounds")]
        public bool useCameraBounds;
        public Bounds cameraBounds = new Bounds(Vector3.zero, new Vector3(100, 100, 100));

        [Header("Screen Constraint")]
        public ScreenConstraintMode screenConstraintMode = ScreenConstraintMode.None;
        [Tooltip("Orthographic: fixed world-width the camera must show")]
        public float constrainedWidth = 10f;
        [Tooltip("Perspective: world-space rect size the camera must keep visible")]
        public Vector2 constrainedVisibleArea = new(10f, 6f);
        [Tooltip("Perspective: world point the visible area is centered on")]
        public Vector3 constrainedAreaTarget;
    }
}

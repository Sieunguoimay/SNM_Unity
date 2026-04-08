using UnityEngine;

namespace Snm.CameraRig
{
    public static class CameraConfigLerp
    {
        public static void Lerp(CameraRigConfig from, CameraRigConfig to, float t, CameraRigConfig result)
        {
            result.convergenceRateXY = Mathf.Lerp(from.convergenceRateXY, to.convergenceRateXY, t);
            result.convergenceRateZ = Mathf.Lerp(from.convergenceRateZ, to.convergenceRateZ, t);
            result.convergenceRateRotation = Mathf.Lerp(from.convergenceRateRotation, to.convergenceRateRotation, t);
            result.deadZoneX = Mathf.Lerp(from.deadZoneX, to.deadZoneX, t);
            result.deadZoneY = Mathf.Lerp(from.deadZoneY, to.deadZoneY, t);
            result.ndcPaddingX = Mathf.Lerp(from.ndcPaddingX, to.ndcPaddingX, t);
            result.ndcPaddingY = Mathf.Lerp(from.ndcPaddingY, to.ndcPaddingY, t);
            result.minDistance = Mathf.Lerp(from.minDistance, to.minDistance, t);
            result.maxDistance = Mathf.Lerp(from.maxDistance, to.maxDistance, t);
            result.enableManualZoom = t < 0.5f ? from.enableManualZoom : to.enableManualZoom;
            result.zoomSmoothRate = Mathf.Lerp(from.zoomSmoothRate, to.zoomSmoothRate, t);
            result.zoomSensitivity = Mathf.Lerp(from.zoomSensitivity, to.zoomSensitivity, t);
            result.lookAheadFactor = Mathf.Lerp(from.lookAheadFactor, to.lookAheadFactor, t);
            result.lookAheadMaxOffset = Mathf.Lerp(from.lookAheadMaxOffset, to.lookAheadMaxOffset, t);
            result.defaultPitch = Mathf.Lerp(from.defaultPitch, to.defaultPitch, t);
            result.pitchConvergenceRate = Mathf.Lerp(from.pitchConvergenceRate, to.pitchConvergenceRate, t);
            result.useCameraBounds = t < 0.5f ? from.useCameraBounds : to.useCameraBounds;
            result.cameraBounds = new Bounds(
                Vector3.Lerp(from.cameraBounds.center, to.cameraBounds.center, t),
                Vector3.Lerp(from.cameraBounds.size, to.cameraBounds.size, t));
            result.screenConstraintMode = t < 0.5f ? from.screenConstraintMode : to.screenConstraintMode;
            result.constrainedWidth = Mathf.Lerp(from.constrainedWidth, to.constrainedWidth, t);
            result.constrainedVisibleArea = Vector2.Lerp(from.constrainedVisibleArea, to.constrainedVisibleArea, t);
            result.constrainedAreaTarget = Vector3.Lerp(from.constrainedAreaTarget, to.constrainedAreaTarget, t);
        }
    }
}

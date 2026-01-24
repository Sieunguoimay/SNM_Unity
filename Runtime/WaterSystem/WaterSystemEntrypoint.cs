using System;
using Snm.Runtime.Debugging;
using Snm.Runtime.Dispose;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    [ExecuteInEditMode]
    public class WaterSystemEntrypoint : MonoBehaviour
    {
        private IDisposable _destroyer;

        private void OnEnable()
        {
            if (!isActiveAndEnabled) return;

            var waterSurface = new WaterSurface() { size = new Vector2(10f, 10f) };

            var waterSurfaceMB = UnityEngineUtility.CreateGameObjectWithComponent<WaterSurfaceMB>();
            waterSurfaceMB.SetWaterSurface(waterSurface);

            var mirrorCameraMoveTransform = new GameObject("[MirrorCamera]");

            var mirrorCamera = MirrorCameraCreator.Create();
            MirrorCameraCreator.CopyCameraSetting(Camera.main, mirrorCamera);
            mirrorCamera.transform.SetParent(mirrorCameraMoveTransform.transform);
            mirrorCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var mirrorCameraDebugVisual = UnityEngineUtility.CreateGameObjectWithComponent<MirrorCameraDebugVisualizer>();
            mirrorCameraDebugVisual.SetWaterSurface(waterSurface);
            mirrorCameraDebugVisual.SetCamera(mirrorCamera);

            var mirrorCameraMover = new MirrorCameraMover(
                waterSurface,
                new()
                {
                    target = Camera.main.transform,
                    mirror = mirrorCameraMoveTransform.transform
                });
            var mirrorCameraMoverMB = UnityEngineUtility.CreateGameObjectWithComponent<MirrorCameraMoverMB>();
            mirrorCameraMoverMB.SetMover(mirrorCameraMover);

            _destroyer = new DisposeCallback(() =>
            {
                UnityEngineUtility.DestroyGameObject(mirrorCameraDebugVisual.gameObject);
                UnityEngineUtility.DestroyGameObject(mirrorCameraMoverMB.gameObject);
                UnityEngineUtility.DestroyGameObject(waterSurfaceMB.gameObject);
                UnityEngineUtility.DestroyGameObject(mirrorCamera.gameObject);
                UnityEngineUtility.DestroyGameObject(mirrorCameraMoveTransform);
            });
        }

        private void OnDisable()
        {
            _destroyer?.Dispose();
            _destroyer = null;
        }
    }
}
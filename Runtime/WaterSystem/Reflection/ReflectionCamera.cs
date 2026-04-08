// ─────────────────────────────────────────────
// ReflectionCamera.cs
// Owns the reflected camera GameObject and keeps it mirrored
// to a source camera across a water plane.
// ─────────────────────────────────────────────
using System;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public class ReflectionCamera : IDisposable
    {
        private readonly Transform mirrorRoot;   // receives the reflected transform
        private readonly Camera camera;

        public Camera Camera => camera;

        public ReflectionCamera(Camera sourceCamera)
        {
            mirrorRoot = UnityEngineUtility
                .CreateGameObjectWithComponent<Transform>("[ReflectionRoot]");

            camera = UnityEngineUtility
                .CreateGameObjectWithComponent<Camera>("[ReflectionCamera]");

            ConfigureCamera(camera);
            CopyLensSettings(sourceCamera, camera);

            camera.transform.SetParent(mirrorRoot);
            camera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            camera.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            if(camera) UnityEngineUtility.DestroyObject(camera.gameObject);
            if(mirrorRoot) UnityEngineUtility.DestroyObject(mirrorRoot.gameObject);
        }

        /// Reposition the mirror root so the camera appears reflected across the plane.
        public void MirrorAcross(Transform source, in ReflectionPlane plane)
        {
            mirrorRoot.position = plane.ReflectPoint(source.position);
            mirrorRoot.rotation = Quaternion.LookRotation(
                plane.ReflectDirection(source.forward),
                plane.ReflectDirection(source.up));
        }

        /// Sync lens settings that may change at runtime (zoom, clip planes).
        public void SyncLensSettings(Camera source)
        {
            CopyLensSettings(source, camera);
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static void ConfigureCamera(Camera camera)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            camera.depthTextureMode = DepthTextureMode.None;
            camera.renderingPath = RenderingPath.Forward;
        }

        private static void CopyLensSettings(Camera source, Camera destination)
        {
            destination.orthographic = source.orthographic;
            destination.orthographicSize = source.orthographicSize;
            destination.fieldOfView = source.fieldOfView;
            destination.nearClipPlane = source.nearClipPlane;
            destination.farClipPlane = source.farClipPlane;
            destination.aspect = source.aspect;
        }
    }
}
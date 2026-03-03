using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class ReflectionCameraCreator
    {
        public static Camera Create()
        {
            var camera = UnityEngineUtility.CreateGameObjectWithComponent<Camera>("[ReflectionCamera]");
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0, 0, 0, 0);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            camera.depthTextureMode = DepthTextureMode.None;
            camera.renderingPath = RenderingPath.Forward;
            camera.gameObject.SetActive(false);
            return camera;
        }

        public static void CopyCameraSetting(Camera from, Camera to)
        {
            to.fieldOfView = from.fieldOfView;
            to.nearClipPlane = from.nearClipPlane;
            to.farClipPlane = from.farClipPlane;
            to.aspect = from.aspect;
        }
    }
}
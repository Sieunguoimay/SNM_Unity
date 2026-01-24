using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class MirrorCameraCreator
    {
        public static Camera Create()
        {
            var camera = UnityEngineUtility.CreateGameObjectWithComponent<Camera>("[ReflectionCamera]");
            camera.clearFlags = CameraClearFlags.Nothing;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            camera.depthTextureMode = DepthTextureMode.None;
            camera.renderingPath = RenderingPath.Forward;
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
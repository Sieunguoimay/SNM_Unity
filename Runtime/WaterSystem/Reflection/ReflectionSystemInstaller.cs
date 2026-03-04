using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{

    public static class ReflectionSystemInstaller
    {
        public static ReflectionSystem Install(
            WaterSystemConfig config, 
            WaterSurface waterSurface,
            Camera cam)
        {
            var reflectionCameraMoveTransform = UnityEngineUtility.CreateGameObjectWithComponent<Transform>("[ReflectionCamera]");
            var reflectionCamera = ReflectionCameraCreator.Create();

            ReflectionCameraCreator.CopyCameraSetting(cam, reflectionCamera);
            reflectionCamera.transform.SetParent(reflectionCameraMoveTransform);
            reflectionCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var reflectionCameraDebugVisual = UnityEngineUtility.CreateGameObjectWithComponent<ReflectionCameraDebugVisualizer>();
            reflectionCameraDebugVisual.SetWaterSurface(waterSurface);
            reflectionCameraDebugVisual.SetCamera(reflectionCamera);

            var reflectionCameraMover = new TransformReflectionMover(
                waterSurface,
                target: cam.transform,
                reflection: reflectionCameraMoveTransform
            );

            var aspect = reflectionCamera.aspect;
            var reflectionRTWidth = config.reflectionTextureWidth;
            int reflectionRTHeight = Mathf.CeilToInt(reflectionRTWidth / aspect);
            var reflectionRT = new RenderTexture(reflectionRTWidth, reflectionRTHeight, 16);
            var previewReflectionTexture = new PreviewReflectionTexture(reflectionRT);
            var reflectionMatrixData = new ReflectionMatrixData();
            var reflectionRenderer = new ReflectionCameraRenderer(
                reflectionMatrixData, reflectionCamera, reflectionRT, new DefaultCameraRenderExecutor(),
                textureChangeCallback: () => previewReflectionTexture.InvokeUpdated());
            var targetCamMoveDetector = new TransformChangeDetector(cam.transform, .01f, .1f);
            var reflectionRenderController = new WaterReflectionRenderController(reflectionRenderer, 4);

            var reflectionMatrixDataUpdater = new ReflectionMatrixDataUpdater(
                waterSurface, reflectionCamera, reflectionMatrixData);

            var updater = new ReflectionCameraUpdater(
                targetCamMoveDetector, reflectionCameraMover, reflectionMatrixDataUpdater, reflectionRenderController);

            updater.Initialize();

            var updaterMB = UnityEngineUtility.CreateGameObjectWithComponent<WaterSystemUpdaterMB>();
            updaterMB.AddUpdateTarget(updater);
            updaterMB.AddLateUpdateTarget(reflectionRenderController);

            return new ReflectionSystem(disposeCallback: () =>
            {
                UnityEngineUtility.DestroyObject(updaterMB.gameObject);
                reflectionRT.Release();
                UnityEngineUtility.DestroyObject(reflectionRT);
                UnityEngineUtility.DestroyObject(reflectionCameraDebugVisual.gameObject);
                UnityEngineUtility.DestroyObject(reflectionCamera.gameObject);
                UnityEngineUtility.DestroyObject(reflectionCameraMoveTransform.gameObject);
            },
            reflectionRT, reflectionMatrixData, previewReflectionTexture, reflectionMatrixDataUpdater);
        }
    }
}
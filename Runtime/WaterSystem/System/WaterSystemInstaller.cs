using Snm.Runtime.Dispose;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{

    public static class WaterSystemInstaller
    {
        public static WaterSystemHandle Install(Object context, WaterSystemConfig config)
        {
            var size = config.waterSurfaceSize;
            var waterSurface = new WaterSurface()
            {
                size = size,
                mesh = config.autoGenerateMesh ? WaterSurfaceMeshBuilder.CreateQuadMesh(size) : config.mesh
            };

            var waterSurfaceMB = UnityEngineUtility.CreateGameObjectWithComponent<WaterSurfaceMB>();
            waterSurfaceMB.SetWaterSurface(waterSurface);

            var mirrorCameraMoveTransform = UnityEngineUtility.CreateGameObjectWithComponent<Transform>("[MirrorCamera]");
            var mirrorCamera = MirrorCameraCreator.Create();
            MirrorCameraCreator.CopyCameraSetting(Camera.main, mirrorCamera);
            mirrorCamera.transform.SetParent(mirrorCameraMoveTransform);
            mirrorCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var mirrorCameraDebugVisual = UnityEngineUtility.CreateGameObjectWithComponent<MirrorCameraDebugVisualizer>();
            mirrorCameraDebugVisual.SetWaterSurface(waterSurface);
            mirrorCameraDebugVisual.SetCamera(mirrorCamera);

            var mirrorCameraMover = new TransformMirroringMover(
                waterSurface,
                target: Camera.main.transform,
                mirror: mirrorCameraMoveTransform
            );

            var aspect = mirrorCamera.aspect;
            var reflectionRTWidth = config.reflectionTextureWidth;
            int reflectionRTHeight = Mathf.CeilToInt(reflectionRTWidth / aspect);
            var reflectionRT = new RenderTexture(reflectionRTWidth, reflectionRTHeight, 16);
            var previewReflectionTexture = new PreviewReflectionTexture(reflectionRT);
            var reflectionMatrixData = new ReflectionMatrixData();
            var reflectionRenderer = new MirrorringCameraRenderer(
                reflectionMatrixData, mirrorCamera, reflectionRT, new DefaultCameraRenderExecutor(),
                textureChangeCallback: () => previewReflectionTexture.InvokeUpdated());
            var targetCamMoveDetector = new TransformChangeDetector(Camera.main.transform, .01f, .1f);
            var reflectionRenderController = new WaterReflectionRenderController(reflectionRenderer, 4);

            //render water surface
            var surfacePresenter = new WaterSurfacePresenter(waterSurface, config.waterSurfaceShader);
            surfacePresenter.SetReflectionTex(reflectionRT);

            var reflectionMatrixDataUpdater = new ReflectionMatrixDataUpdater(
                waterSurface, mirrorCamera, reflectionMatrixData,
                dataChangeCallback: () => surfacePresenter.SetReflectionVPMatrix(reflectionMatrixData.VP));

            var updater = new MirroringCameraUpdater(
                targetCamMoveDetector, mirrorCameraMover, reflectionMatrixDataUpdater, reflectionRenderController);

            var updaterMB = UnityEngineUtility.CreateGameObjectWithComponent<WaterSystemUpdaterMB>();
            updaterMB.AddUpdateTarget(updater);
            updaterMB.AddUpdateTarget(surfacePresenter);
            updaterMB.AddLateUpdateTarget(reflectionRenderController);

            return new(new DisposeCallback(() =>
            {
                surfacePresenter.Cleanup();
                UnityEngineUtility.DestroyObject(reflectionRT);
                UnityEngineUtility.DestroyObject(mirrorCameraDebugVisual.gameObject);
                UnityEngineUtility.DestroyObject(updaterMB.gameObject);
                UnityEngineUtility.DestroyObject(waterSurfaceMB.gameObject);
                UnityEngineUtility.DestroyObject(mirrorCamera.gameObject);
                UnityEngineUtility.DestroyObject(mirrorCameraMoveTransform.gameObject);
            }), previewReflectionTexture);
        }
    }
}
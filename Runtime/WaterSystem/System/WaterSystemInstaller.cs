using System;
using Snm.Runtime.Dispose;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public static class WaterSystemInstaller
    {
        public static WaterSystemHandle Install(GameObject context, WaterSystemConfig config)
        {
            var size = config.waterSurfaceSize;
            var waterSurface = new WaterSurface()
            {
                size = size,
                mesh = config.autoGenerateMesh ? WaterSurfaceMeshBuilder.CreateQuadMesh(size) : config.mesh
            };

            var waterSurfaceMB = UnityEngineUtility.CreateGameObjectWithComponent<WaterSurfaceMB>();
            waterSurfaceMB.SetWaterSurface(waterSurface);
            waterSurfaceMB.transform.SetParent(context.transform);
            waterSurfaceMB.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var reflectionSystem = ReflectionSystemInstaller.Install(config, waterSurface);
            //render water surface
            Material material = null;
            var surfacePresenter = new WaterSurfacePresenter(waterSurface, config.waterSurfaceMaterial ?? (material = new Material(config.waterSurfaceShader)));
            surfacePresenter.SetReflectionTex(reflectionSystem.reflectionRT);

            var reflectionMatrixDataUpdater = new ReflectionMatrixDataUpdater(
                waterSurface, reflectionSystem.reflectionCamera, reflectionSystem.reflectionMatrixData,
                dataChangeCallback: () => surfacePresenter.SetReflectionVPMatrix(reflectionSystem.reflectionMatrixData.VP));

            var updater = new ReflectionCameraUpdater(
                reflectionSystem.targetCamMoveDetector, reflectionSystem.reflectionCameraMover, reflectionMatrixDataUpdater, reflectionSystem.reflectionRenderController);
            updater.Initialize();

            var updaterMB = UnityEngineUtility.CreateGameObjectWithComponent<WaterSystemUpdaterMB>();
            updaterMB.AddUpdateTarget(updater);
            updaterMB.AddUpdateTarget(surfacePresenter);
            updaterMB.AddLateUpdateTarget(reflectionSystem.reflectionRenderController);

            return new(new DisposeCallback(() =>
            {
                surfacePresenter.Cleanup();
                if (material != null) UnityEngineUtility.DestroyObject(material);
                UnityEngineUtility.DestroyObject(updaterMB.gameObject);
                UnityEngineUtility.DestroyObject(waterSurfaceMB.gameObject);
            }), reflectionSystem.previewReflectionTexture);
        }
    }
}
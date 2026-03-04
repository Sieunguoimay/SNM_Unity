using Snm.Runtime.Dispose;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public static class WaterSystemInstaller
    {
        public static WaterSystemHandle Install(
            GameObject context, 
            WaterSystemConfig config, Camera cam)
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

            //render water surface
            Material material = null;
            var surfacePresenter = new WaterSurfacePresenter(waterSurface, config.waterSurfaceMaterial ?? (material = new Material(config.waterSurfaceShader)));
            var reflectionSystem = ReflectionSystemInstaller.Install(config, waterSurface, cam);

            surfacePresenter.SetReflectionTex(reflectionSystem.ReflectionRT);
            reflectionSystem.OnReflectionVPChanged += vp => surfacePresenter.SetReflectionVPMatrix(vp);

            var updaterMB = UnityEngineUtility.CreateGameObjectWithComponent<WaterSystemUpdaterMB>();
            updaterMB.AddUpdateTarget(surfacePresenter);

            return new(new DisposeCallback(() =>
            {
                surfacePresenter.Cleanup();
                reflectionSystem.Dispose();
                if (material != null) UnityEngineUtility.DestroyObject(material);
                UnityEngineUtility.DestroyObject(updaterMB.gameObject);
                UnityEngineUtility.DestroyObject(waterSurfaceMB.gameObject);
            }), reflectionSystem.PreviewReflectionTexture);
        }
    }
}
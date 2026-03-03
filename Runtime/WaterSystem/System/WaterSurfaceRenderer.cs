using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class WaterSurfacePresenter : IUpdateTarget
    {
        private readonly Material material;
        private readonly WaterSurfaceView view;
        private readonly WaterSurfaceMaterialBinder matBinder;
        private readonly WaterSurface waterSurface;

        public WaterSurfacePresenter(
            WaterSurface waterSurface,
            Material material)
        {
            this.waterSurface = waterSurface;
            this.material = material;

            view = new WaterSurfaceView(waterSurface.mesh, material);
            matBinder = new WaterSurfaceMaterialBinder(material);
        }

        public void Cleanup()
        {
            view.Destroy();
        }

        public void Update()
        {
            view.SetTransform(waterSurface.position, waterSurface.rotation);
        }

        public void SetReflectionTex(Texture reflectionMap)
        {
            matBinder.SetReflectionTex(reflectionMap);
        }

        public void SetReflectionVPMatrix(Matrix4x4 vp)
        {
            matBinder.SetReflectionVPMatrix(vp);
        }
    }

    public class WaterSurfaceView
    {
        private readonly GameObject mrGo;

        public WaterSurfaceView(
            Mesh mesh,
            Material material)
        {
            var mr = UnityEngineUtility.CreateGameObjectWithComponent<MeshRenderer>();
            var mf = mr.gameObject.AddComponent<MeshFilter>();

            mf.sharedMesh = mesh;
            mr.sharedMaterial = material;

            mrGo = mr.gameObject;
        }

        public void Destroy()
        {
            UnityEngineUtility.DestroyObject(mrGo);
        }

        public void SetTransform(Vector3 position, Quaternion rotation)
        {
            mrGo.transform.SetPositionAndRotation(position, rotation);
        }
    }

    public class WaterSurfaceMaterialBinder
    {
        private static readonly int ReflectionTexID = Shader.PropertyToID("_ReflectionTex");
        private static readonly int ReflectionVPID = Shader.PropertyToID("_ReflectionVP");
        private readonly Material material;

        public WaterSurfaceMaterialBinder(Material material)
        {
            this.material = material;
        }

        public void SetReflectionTex(Texture reflectionMap)
        {
            material.SetTexture(ReflectionTexID, reflectionMap);
        }

        public void SetReflectionVPMatrix(Matrix4x4 vp)
        {
            material.SetMatrix(ReflectionVPID, vp);
        }
    }
}
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public class ReflectionShaderBinder
    {
        private static readonly int ReflectionVPID = Shader.PropertyToID("_ReflectionVP");

        private readonly Material _material;
        private readonly ReflectionCamera _reflectionCamera;

        public ReflectionShaderBinder(Material material, ReflectionCamera reflectionCamera)
        {
            _material = material;
            _reflectionCamera = reflectionCamera;
        }

        public void Bind(Matrix4x4 projection)
        {
            var vp = projection * _reflectionCamera.Camera.worldToCameraMatrix;
            _material.SetMatrix(ReflectionVPID, vp);
        }
    }
}

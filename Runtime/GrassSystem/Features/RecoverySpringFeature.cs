using UnityEngine;

namespace Snm.GrassSystem
{
    public class RecoverySpringFeature : IGrassFeature
    {
        readonly GrassFeatureContext _ctx;

        public RecoverySpringFeature(GrassFeatureContext ctx)
        {
            _ctx = ctx;
            BindConfig();
        }

        public void OnUpdate(float deltaTime)
        {
            BindConfig();
        }

        public void Dispose() { }

        void BindConfig()
        {
            var config = _ctx.Config.trample;
            foreach (var mat in _ctx.AllMaterials)
            {
                mat.SetFloat(ShaderIDs.SpringFrequency, config.springFrequency);
                mat.SetFloat(ShaderIDs.SpringDamping, config.springDamping);
                mat.SetFloat(ShaderIDs.SpringAmplitude, config.springEnabled ? config.springAmplitude : 0f);
            }
        }

        static class ShaderIDs
        {
            public static readonly int SpringFrequency = Shader.PropertyToID("_SpringFrequency");
            public static readonly int SpringDamping = Shader.PropertyToID("_SpringDamping");
            public static readonly int SpringAmplitude = Shader.PropertyToID("_SpringAmplitude");
        }
    }
}

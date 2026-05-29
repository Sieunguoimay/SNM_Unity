using UnityEngine;

namespace Snm.GrassSystem
{
    public class RecoverySpringFeature : IGrassFeature
    {
        readonly GrassFeatureContext _ctx;

        bool _applied;
        float _lastFrequency;
        float _lastDamping;
        float _lastAmplitude;

        public RecoverySpringFeature(GrassFeatureContext ctx)
        {
            _ctx = ctx;
            BindIfDirty();
        }

        public void OnUpdate(float deltaTime)
        {
            BindIfDirty();
        }

        public void Dispose() { }

        void BindIfDirty()
        {
            var config = _ctx.Config.trample;
            float amplitude = config.springEnabled ? config.springAmplitude : 0f;

            if (_applied && _lastFrequency == config.springFrequency && _lastDamping == config.springDamping && _lastAmplitude == amplitude)
                return;

            _lastFrequency = config.springFrequency;
            _lastDamping = config.springDamping;
            _lastAmplitude = amplitude;
            _applied = true;

            foreach (var mat in _ctx.AllMaterials)
            {
                mat.SetFloat(ShaderIDs.SpringFrequency, config.springFrequency);
                mat.SetFloat(ShaderIDs.SpringDamping, config.springDamping);
                mat.SetFloat(ShaderIDs.SpringAmplitude, amplitude);
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

using UnityEngine;

namespace Snm.GrassSystem
{
    public class WindFeature : IGrassFeature
    {
        readonly GrassFeatureContext _ctx;

        public WindFeature(GrassFeatureContext ctx)
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
            var config = _ctx.Config.wind;
            var windParams = new Vector4(
                config.windStrength,
                config.windScrollSpeed,
                config.windMapScale.x,
                config.windMapScale.y);

            foreach (var mat in _ctx.AllMaterials)
            {
                mat.SetTexture(ShaderIDs.WindMap, config.windMap);
                mat.SetVector(ShaderIDs.WindParams, windParams);
            }
        }

        static class ShaderIDs
        {
            public static readonly int WindMap = Shader.PropertyToID("_WindMap");
            public static readonly int WindParams = Shader.PropertyToID("_WindParams");
        }
    }
}

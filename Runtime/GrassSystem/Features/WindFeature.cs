using System.Collections.Generic;
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

            var windParams2 = new Vector4(
                config.swayVariation,
                config.amplitudeVariation,
                0, 0);

            foreach (var mat in _ctx.AllMaterials)
            {
                mat.SetTexture(ShaderIDs.WindMap, config.windMap);
                mat.SetVector(ShaderIDs.WindParams, windParams);
                mat.SetVector(ShaderIDs.WindParams2, windParams2);
            }
        }

        public static void ClearWindProperties(IEnumerable<Material> materials)
        {
            foreach (var mat in materials)
            {
                mat.SetTexture(ShaderIDs.WindMap, null);
                mat.SetVector(ShaderIDs.WindParams, Vector4.zero);
                mat.SetVector(ShaderIDs.WindParams2, Vector4.zero);
            }
        }

        static class ShaderIDs
        {
            public static readonly int WindMap = Shader.PropertyToID("_WindMap");
            public static readonly int WindParams = Shader.PropertyToID("_WindParams");
            public static readonly int WindParams2 = Shader.PropertyToID("_WindParams2");
        }
    }
}

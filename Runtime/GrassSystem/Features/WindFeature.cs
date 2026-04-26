using System.Collections.Generic;
using UnityEngine;

namespace Snm.GrassSystem
{
    public class WindFeature : IGrassFeature
    {
        readonly GrassFeatureContext _ctx;

        bool _applied;
        Texture _lastMap;
        Vector4 _lastParams;
        Vector4 _lastParams2;

        public WindFeature(GrassFeatureContext ctx)
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
            var config = _ctx.Config.wind;
            var p1 = new Vector4(
                config.windStrength,
                config.windScrollSpeed,
                config.windMapScale.x,
                config.windMapScale.y);
            var p2 = new Vector4(
                config.swayVariation,
                config.amplitudeVariation,
                0, 0);

            if (_applied && _lastMap == config.windMap && _lastParams == p1 && _lastParams2 == p2)
                return;

            _lastMap = config.windMap;
            _lastParams = p1;
            _lastParams2 = p2;
            _applied = true;

            foreach (var mat in _ctx.AllMaterials)
            {
                mat.SetTexture(ShaderIDs.WindMap, config.windMap);
                mat.SetVector(ShaderIDs.WindParams, p1);
                mat.SetVector(ShaderIDs.WindParams2, p2);
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

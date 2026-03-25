using UnityEngine;

namespace Snm.GrassSystem
{
    public class WindFeature : IGrassFeature
    {
        public WindFeature(GrassFeatureContext ctx)
        {
            var config = ctx.Config.wind;
            var mat = ctx.GrassMaterial;

            mat.SetTexture(ShaderIDs.WindMap, config.windMap);
            mat.SetVector(ShaderIDs.WindParams, new Vector4(
                config.windStrength,
                config.windScrollSpeed,
                config.windMapScale.x,
                config.windMapScale.y));
        }

        public void OnUpdate(float deltaTime) { }
        public void Dispose() { }

        static class ShaderIDs
        {
            public static readonly int WindMap = Shader.PropertyToID("_WindMap");
            public static readonly int WindParams = Shader.PropertyToID("_WindParams");
        }
    }
}

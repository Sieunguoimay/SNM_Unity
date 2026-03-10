using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public class WaveDisplayPass : IWaveDisplayPass
    {
        private readonly Material material;

        private readonly int ID_HeightfieldTex = Shader.PropertyToID("_HeightfieldTex");
        private readonly int ID_HeightfieldStrength = Shader.PropertyToID("_HeightfieldStrength");
        private readonly int ID_DisplayMode = Shader.PropertyToID("_DisplayMode");

        public WaveDisplayPass(Material material)
        {
            this.material = material;
        }

        public void Render(
            RenderTexture waveTexture,
            RenderTexture heightfield,
            RenderTexture target,
            float heightfieldStrength,
            int displayMode)
        {
            material.SetTexture(ID_HeightfieldTex, heightfield);
            material.SetFloat(ID_HeightfieldStrength, heightfieldStrength);
            material.SetFloat(ID_DisplayMode, displayMode);

            Graphics.Blit(waveTexture, target, material);
        }

        public void Dispose()
        {
            if (material != null)
                UnityEngineUtility.DestroyObject(material);
        }
    }
}

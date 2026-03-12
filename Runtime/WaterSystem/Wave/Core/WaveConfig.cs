using System;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    [Serializable]
    public class WaveConfig
    {
        public bool enabled = true;
        public Shader simulationShader;
        public Shader displayShader;
        public int textureSize = 512;
    }
}

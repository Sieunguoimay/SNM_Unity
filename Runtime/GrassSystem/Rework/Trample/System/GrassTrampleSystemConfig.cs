using System;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [Serializable]
    public class GrassTrampleSystemConfig
    {
        public bool enabled = true;
        public Shader shader;
        public float brushMinOffset = 0.01f;
        public float fadeSpeed = 0.1f;
    }
}
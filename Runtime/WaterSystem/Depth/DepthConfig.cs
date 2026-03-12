using System;
using UnityEngine;

namespace Snm.WaterSystem.Depth
{
    [Serializable]
    public class DepthConfig
    {
        public bool enabled = true;
        public Color shallowColor = Color.white;
        public Color deepColor = Color.black;

        [Range(0f, 2f)]
        public float absorption = 0.4f;
    }
}

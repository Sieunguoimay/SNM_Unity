using System;
using UnityEngine;

namespace Snm.WaterSystem.IntersectionBands
{
    [Serializable]
    public class IntersectionBandsConfig
    {
        public bool enabled;

        [Range(1, 10)]
        public int lineCount = 4;

        [Range(0f, 3f)]
        public float speed = 0.5f;

        [Range(0f, 1f)]
        public float strength = 0.5f;

        [Tooltip("Higher values produce thinner lines.")]
        [Range(1f, 20f)]
        public float sharpness = 8f;

        [Tooltip("Maximum depth where contour lines appear.")]
        [Range(0.5f, 20f)]
        public float maxDepth = 5f;
    }
}

using System;
using UnityEngine;

namespace Snm.WaterSystem.Sparkle
{
    [Serializable]
    public class SparkleConfig
    {
        public bool enabled;

        [Range(0f, 5f)]
        public float intensity = 1f;

        [Range(1f, 100f)]
        public float density = 30f;

        [Range(0f, 2f)]
        public float speed = 0.5f;
    }
}

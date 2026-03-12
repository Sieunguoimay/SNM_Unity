using System;
using UnityEngine;

namespace Snm.WaterSystem.ScrollNormal
{
    [Serializable]
    public class ScrollNormalConfig
    {
        public bool enabled;
        public Texture2D normalMap;

        [Range(0f, 2f)]
        public float strength = 0.5f;

        [Range(0.01f, 2f)]
        public float scale = 0.2f;

        [Tooltip("Scroll direction and speed for the first normal layer.")]
        public Vector2 speed1 = new(0.03f, 0.02f);

        [Tooltip("Scroll direction and speed for the second normal layer.")]
        public Vector2 speed2 = new(-0.02f, 0.03f);
    }
}

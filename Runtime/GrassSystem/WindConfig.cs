using System;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [Serializable]
    public class WindConfig
    {
        public Texture2D dudvMap;
        public Vector2 mapSize = new(10, 10);
        public float scrollSpeed = .01f;
        public float strength = 1f;
    }
}
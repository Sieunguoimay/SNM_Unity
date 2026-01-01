using System;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [Serializable]
    public class WindData
    {
        public Texture2D dudvMap;
        public Vector2 mapSize = new(10, 10);
        public float strength = 1f;
        public float scrollSpeed = .01f;
    }
}
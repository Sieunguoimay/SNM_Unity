using System.Collections.Generic;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class AnimationInstancingRendererManager : MonoBehaviour
    {
        private static AnimationInstancingRendererManager _instance;
        public static AnimationInstancingRendererManager Instance
        {
            get
            {
                if (_destroyed) return null;
                if (_instance == null)
                {
                    _instance = new GameObject("#AnimationInstancingRendererManager")
                        .AddComponent<AnimationInstancingRendererManager>();
                }
                return _instance;
            }
        }

        private static bool _destroyed = false;
        public Dictionary<int, VertexCache> vertexCachePool = new();

        private void OnDestroy()
        {
            _destroyed = true;
        }

    }
}
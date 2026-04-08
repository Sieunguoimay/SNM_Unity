using System.Collections.Generic;
using UnityEngine;

namespace Snm.CameraRig
{
    public class CameraShakeState
    {
        private struct ShakeInstance
        {
            public float intensity;
            public float duration;
            public float elapsed;
            public float frequency;
            public float seedX;
            public float seedY;
            public float seedZ;
        }

        private readonly List<ShakeInstance> _active = new();

        public bool HasActiveShakes => _active.Count > 0;

        public void AddShake(float intensity, float duration, float frequency = 25f)
        {
            _active.Add(new ShakeInstance
            {
                intensity = intensity,
                duration = duration,
                elapsed = 0f,
                frequency = frequency,
                seedX = Random.value * 1000f,
                seedY = Random.value * 1000f,
                seedZ = Random.value * 1000f,
            });
        }

        public Vector3 Evaluate(float dt)
        {
            var offset = Vector3.zero;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var s = _active[i];
                s.elapsed += dt;

                if (s.elapsed >= s.duration)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                _active[i] = s;

                // Decay intensity linearly over duration
                var t = s.elapsed / s.duration;
                var currentIntensity = s.intensity * (1f - t);

                var time = s.elapsed * s.frequency;
                offset.x += (Mathf.PerlinNoise(s.seedX + time, 0f) * 2f - 1f) * currentIntensity;
                offset.y += (Mathf.PerlinNoise(s.seedY + time, 0f) * 2f - 1f) * currentIntensity;
                offset.z += (Mathf.PerlinNoise(s.seedZ + time, 0f) * 2f - 1f) * currentIntensity * 0.5f;
            }

            return offset;
        }
    }
}

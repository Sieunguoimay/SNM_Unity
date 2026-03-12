using System.Collections.Generic;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public class DisturbanceBuffer
    {
        private const int MAX = 32;

        private readonly List<WaveDisturbance> disturbances = new();
        private readonly Vector4[] vectorBuffer = new Vector4[MAX];

        public void Add(WaveDisturbance disturbance)
        {
            if (disturbances.Count < MAX)
            {
                disturbances.Add(disturbance);
            }
            else
            {
                Debug.LogWarning($"[WaveSystem] Disturbance buffer full ({MAX}). Disturbance dropped.");
            }
        }

        public void Upload(Material material, int idArray, int idCount)
        {
            for (int i = 0; i < MAX; i++)
                vectorBuffer[i] = Vector4.zero;

            int count = Mathf.Min(disturbances.Count, MAX);

            for (int i = 0; i < count; i++)
            {
                var d = disturbances[i];
                vectorBuffer[i] = new Vector4(d.uvPos.x, d.uvPos.y, d.radius, d.strength);
            }

            disturbances.Clear();

            material.SetVectorArray(idArray, vectorBuffer);
            material.SetFloat(idCount, count);
        }
    }
}

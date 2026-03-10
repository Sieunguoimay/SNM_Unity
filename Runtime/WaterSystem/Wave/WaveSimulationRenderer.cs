using System;
using System.Collections.Generic;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public class WaveSimulationRenderer : IDisposable, IUpdateTarget
    {
        private const int MAX_DISTURBANCES = 32;

        private readonly List<WaveDisturbance> disturbances = new();
        private readonly Vector4[] vectorBuffer = new Vector4[MAX_DISTURBANCES];
        private readonly RenderTexture secondTexture;
        private readonly Material material;
        private readonly RenderTexture renderTexture;
        private readonly IUpdateService updateService;

        private bool _pingPongValue;

        // Shader property IDs
        private readonly int ID_Disturbances = Shader.PropertyToID("_Disturbances");
        private readonly int ID_DisturbanceCount = Shader.PropertyToID("_DisturbanceCount");
        private readonly int ID_Damping = Shader.PropertyToID("_Damping");
        private readonly int ID_WaveSpeed = Shader.PropertyToID("_WaveSpeed");

        public float damping = 0.99f;
        public float waveSpeed = 0.5f;

        public WaveSimulationRenderer(
            RenderTexture renderTexture,
            Shader waveSimulationShader,
            IUpdateService updateService)
        {
            this.renderTexture = renderTexture;
            this.updateService = updateService;

            secondTexture = new RenderTexture(renderTexture.descriptor);
            material = new Material(waveSimulationShader);

            // Initialize buffer with zeros
            ClearVectorBuffer();

            this.updateService.AddUpdateTarget(this);
        }

        public void Dispose()
        {
            updateService.RemoveUpdateTarget(this);

            UnityEngineUtility.DestroyObject(material);
            secondTexture.Release();
            UnityEngineUtility.DestroyObject(secondTexture);
        }

        public void Update()
        {
            UploadDisturbances();
            Render();
        }

        public void Render()
        {
            material.SetFloat(ID_Damping, damping);
            material.SetFloat(ID_WaveSpeed, waveSpeed);

            var rtA = renderTexture;
            var rtB = secondTexture;

            var src = _pingPongValue ? rtA : rtB;
            var dst = _pingPongValue ? rtB : rtA;

            Graphics.Blit(src, dst, material);

            _pingPongValue = !_pingPongValue;
        }

        public void AddDisturbance(WaveDisturbance disturbance)
        {
            if (disturbances.Count < MAX_DISTURBANCES)
            {
                disturbances.Add(disturbance);
            }
        }

        private void ClearVectorBuffer()
        {
            for (int i = 0; i < MAX_DISTURBANCES; i++)
            {
                vectorBuffer[i] = Vector4.zero;
            }
        }

        private void UploadDisturbances()
        {
            // ALWAYS clear the buffer first
            ClearVectorBuffer();

            int count = Mathf.Min(disturbances.Count, MAX_DISTURBANCES);

            // Copy disturbances to buffer
            for (int i = 0; i < count; i++)
            {
                var d = disturbances[i];
                vectorBuffer[i] = new Vector4(d.uvPos.x, d.uvPos.y, d.radius, d.strength);
            }

            disturbances.Clear();

            // Upload to shader
            material.SetVectorArray(ID_Disturbances, vectorBuffer);
            
            // Use float instead of int for reliability
            material.SetFloat(ID_DisturbanceCount, count);
        }
    }
}
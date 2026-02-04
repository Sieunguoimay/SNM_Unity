using System;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class ReflectionMatrixDataUpdater
    {
        private readonly WaterSurface waterSurface;
        private readonly Camera mirroringCamera;
        private readonly Action dataChangeCallback;
        private readonly ReflectionMatrixData reflectionData;

        public ReflectionMatrixDataUpdater(
            WaterSurface waterSurface,
            Camera mirroringCamera, 
            ReflectionMatrixData reflectionData,
            Action dataChangeCallback)
        {
            this.waterSurface = waterSurface;
            this.mirroringCamera = mirroringCamera;
            this.reflectionData = reflectionData;
            this.dataChangeCallback = dataChangeCallback;
        }

        public void Update()
        {
            var proj = WaterReflectionFrustumCalculator.Calculate(waterSurface, mirroringCamera);

            reflectionData.Proj = proj;
            reflectionData.VP = proj * mirroringCamera.worldToCameraMatrix;

            dataChangeCallback?.Invoke();
        }
    }
}
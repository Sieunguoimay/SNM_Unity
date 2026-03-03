using System;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class ReflectionMatrixDataUpdater
    {
        private readonly WaterSurface waterSurface;
        private readonly Camera reflectionCamera;
        private readonly Action dataChangeCallback;
        private readonly ReflectionMatrixData reflectionData;

        public ReflectionMatrixDataUpdater(
            WaterSurface waterSurface,
            Camera reflectionCamera, 
            ReflectionMatrixData reflectionData,
            Action dataChangeCallback)
        {
            this.waterSurface = waterSurface;
            this.reflectionCamera = reflectionCamera;
            this.reflectionData = reflectionData;
            this.dataChangeCallback = dataChangeCallback;
        }

        public void Update()
        {
            var proj = WaterReflectionFrustumCalculator.Calculate(waterSurface, reflectionCamera);

            reflectionData.Proj = proj;
            reflectionData.VP = proj * reflectionCamera.worldToCameraMatrix;

            dataChangeCallback?.Invoke();
        }
    }
}
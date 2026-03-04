using System;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class ReflectionMatrixDataUpdater
    {
        private readonly WaterSurface waterSurface;
        private readonly Camera reflectionCamera;
        private readonly ReflectionMatrixData reflectionData;
        private Action _dataChangeCallback;

        public ReflectionMatrixDataUpdater(
            WaterSurface waterSurface,
            Camera reflectionCamera,
            ReflectionMatrixData reflectionData)
        {
            this.waterSurface = waterSurface;
            this.reflectionCamera = reflectionCamera;
            this.reflectionData = reflectionData;
        }

        public void SetCallback(Action changeCallback)
        {
            _dataChangeCallback = changeCallback;
        }

        public void Update()
        {
            var proj = WaterReflectionFrustumCalculator.Calculate(waterSurface, reflectionCamera);

            reflectionData.Proj = proj;
            reflectionData.VP = proj * reflectionCamera.worldToCameraMatrix;

            _dataChangeCallback?.Invoke();
        }
    }
}
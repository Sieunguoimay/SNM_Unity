using System;
using System.Collections.Generic;
using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassSystemHandle
    {
        private readonly GrassTrampleSystemHandle trampleHandle;
        private readonly Action destroyCallback;

        public GrassSystemConfig Config { get; }
        public GrassField GrassField { get; }
        public SurfaceCanvas Canvas { get; }
        public GrassDisturberTracker DisturberTracker { get; }
        public int InstanceCount { get; }

        public Texture TrampleTexture => trampleHandle.GetTrampleTexture();
        public Texture2D WindTexture => Config?.windConfig?.dudvMap;

        public GrassSystemHandle(
            GrassTrampleSystemHandle trampleHandle,
            Action destroyCallback,
            GrassSystemConfig config,
            GrassField grassField,
            SurfaceCanvas canvas,
            GrassDisturberTracker tracker,
            int instanceCount)
        {
            this.trampleHandle = trampleHandle;
            this.destroyCallback = destroyCallback;
            Config = config;
            GrassField = grassField;
            Canvas = canvas;
            DisturberTracker = tracker;
            InstanceCount = instanceCount;
        }

        public void SetDisturbers(IReadOnlyList<IGrassDisturber> disturbers)
        {
            trampleHandle.SetDisturbers(disturbers);
        }

        public void DestroySystem()
        {
            destroyCallback?.Invoke();
        }
    }
}

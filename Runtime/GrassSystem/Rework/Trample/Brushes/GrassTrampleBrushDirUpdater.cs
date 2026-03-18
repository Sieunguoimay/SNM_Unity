using System.Collections.Generic;
using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleBrushDirUpdater
    {
        private readonly GrassTrampleBrushRegistry brushRegistry;
        private readonly float minOffset;
        private readonly SurfaceCanvas canvas;
        private readonly Dictionary<GrassTrampleBrush, Vector3> previousPositions = new();

        public GrassTrampleBrushDirUpdater(
            GrassTrampleBrushRegistry brushRegistry,
            float minOffset,
            SurfaceCanvas canvas)
        {
            this.brushRegistry = brushRegistry;
            this.minOffset = minOffset;
            this.canvas = canvas;
        }

        public void Update()
        {
            var brushes = brushRegistry.GetBrushes();

            for (int i = 0; i < brushes.Count; i++)
            {
                var brush = brushes[i];

                UpdateBrush(brush);
            }
        }

        public void UpdateBrush(GrassTrampleBrush brush)
        {
            brush.dir = TryCalculateBrushDir(brush);
            brush.isActive = IsValidBrush(brush);
        }

        private bool IsValidBrush(GrassTrampleBrush brush)
        {
            return canvas.Contains(brush.position)
            || (previousPositions.TryGetValue(brush, out var prevPos) && canvas.Contains(prevPos));
        }

        public Vector3 TryCalculateBrushDir(GrassTrampleBrush brush)
        {
            var currPos = brush.position;

            if (previousPositions.TryGetValue(brush, out var prevPos))
            {
                var movement = currPos - prevPos;
                var sqrMagnitude = movement.sqrMagnitude;
                if (sqrMagnitude > minOffset * minOffset)
                {
                    previousPositions[brush] = currPos;
                    return movement.normalized;
                }
            }
            else
            {
                previousPositions[brush] = currPos;
            }
            return brush.dir;
        }
    }
}

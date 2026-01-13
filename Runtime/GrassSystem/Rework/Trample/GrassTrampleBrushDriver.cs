using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleBrushDriver
    {
        private readonly GrassTrampleBrushRegistry brushRegistry;
        private readonly GrassTramplePainter painter;
        private readonly float minOffset;
        private readonly WorldCanvas canvas;
        private readonly WorldCanvasChecker canvasChecker;
        private readonly Dictionary<GrassTrampleBrush, Vector3> previousPositions = new();

        public GrassTrampleBrushDriver(
            GrassTrampleBrushRegistry brushRegistry,
            GrassTramplePainter painter,
            float minOffset,
            WorldCanvas canvas)
        {
            this.brushRegistry = brushRegistry;
            this.painter = painter;
            this.minOffset = minOffset;
            this.canvas = canvas;
            this.canvasChecker = new WorldCanvasChecker(canvas);
        }

        public void Update()
        {
            var brushes = brushRegistry.GetBrushes();

            foreach (var brush in brushes)
            {
                UpdateBrush(brush);
            }
        }

        public void UpdateBrush(GrassTrampleBrush brush)
        {
            // Update individual brush logic here
            var currPos = brush.position;

            if (!canvasChecker.IsInWorldCanvas(currPos)) return;

            if (previousPositions.TryGetValue(brush, out var prevPos))
            {
                var movement = currPos - prevPos;
                var sqrMagnitude = movement.sqrMagnitude;
                if (sqrMagnitude > minOffset * minOffset)
                {
                    var dir = movement.normalized;
                    painter.SetBrush(currPos, brush.radius, new Vector4(dir.x, dir.y, dir.z, brush.strength));
                    previousPositions[brush] = currPos;
                }
            }
            else
            {
                previousPositions[brush] = currPos;
            }
        }
    }
}
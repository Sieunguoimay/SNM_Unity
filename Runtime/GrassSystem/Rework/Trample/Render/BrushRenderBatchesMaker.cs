using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class BrushRenderBatchesMaker
    {
        private readonly GrassTrampleRenderer renderer;
        private readonly GrassTrampleBrushRegistry brushRegistry;
        private readonly int brushesPerBatch;
        private BrushRenderBatch[] _brushBatches = new BrushRenderBatch[0];
        private int _cachedBrushCount;

        public BrushRenderBatchesMaker(
            GrassTrampleRenderer renderer,
            GrassTrampleBrushRegistry brushRegistry, 
            int brushesPerBatch)
        {
            this.renderer = renderer;
            this.brushRegistry = brushRegistry;
            this.brushesPerBatch = brushesPerBatch;
        }

        public void Update()
        {
            var brushes = brushRegistry.GetBrushes();
            var brushCount = brushes.Count;
            if(brushCount != _cachedBrushCount)
            {
                _cachedBrushCount = brushCount;
                ResizeBatches(ref _brushBatches, brushCount, brushesPerBatch);
                renderer.SetBrushBatches(_brushBatches);
            }

            FillBatches(brushes, _brushBatches, brushesPerBatch);
        }

        public static void ResizeBatches(ref BrushRenderBatch[] batches, int brushes, int brushesPerBatch)
        {
            var requiredBatchCount = (brushes + brushesPerBatch - 1) / brushesPerBatch;
            if (batches.Length != requiredBatchCount)
            {
                System.Array.Resize(ref batches, requiredBatchCount);
                for (int i = 0; i < batches.Length; i++)
                {
                    batches[i] ??= new BrushRenderBatch()
                    {
                        brushes_PosDir = new Vector4[brushesPerBatch],
                        brushes_Radius = new float[brushesPerBatch],
                        brushCount = 0
                    };
                }
            }
        }

        public static void FillBatches(
            IReadOnlyList<GrassTrampleBrush> brushes,
            BrushRenderBatch[] brushBatches,
            int brushesPerBatch)
        {
            int batchIndex = 0;

            for (int i = 0; i < brushes.Count; i++)
            {
                var brush = brushes[i];
                var batch = brushBatches[batchIndex];

                if (brush.isActive)
                {
                    var brushIndexInBatch = batch.brushCount;
                    batch.brushes_PosDir[brushIndexInBatch] = new Vector4(brush.position.x, brush.position.z, brush.dir.x, brush.dir.z);
                    batch.brushes_Radius[brushIndexInBatch] = brush.radius;
                    batch.brushCount++;

                    if (brushIndexInBatch >= brushesPerBatch)
                    {
                        batchIndex++;

                        if(batchIndex >= brushBatches.Length)
                            break;
                    }
                }
            }
        }
    }
}
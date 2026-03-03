using System;

namespace Snm.Runtime.WaterSystem
{
    public class ReflectionCameraUpdater : IUpdateTarget
    {
        private readonly TransformChangeDetector targetCamMoveDetector;
        private readonly TransformReflectionMover reflectionCamMover;
        private readonly ReflectionMatrixDataUpdater reflectionDataUpdater;
        private readonly WaterReflectionRenderController renderController;

        public ReflectionCameraUpdater(
            TransformChangeDetector targetCamMoveDetector,
            TransformReflectionMover reflectionCamMover,
            ReflectionMatrixDataUpdater reflectionDataUpdater,
            WaterReflectionRenderController renderController)
        {
            this.targetCamMoveDetector = targetCamMoveDetector;
            this.reflectionCamMover = reflectionCamMover;
            this.reflectionDataUpdater = reflectionDataUpdater;
            this.renderController = renderController;
        }

        public void Initialize()
        {
            reflectionCamMover.Move();
            reflectionDataUpdater.Update();
        }

        public void Update()
        {
            var changed = targetCamMoveDetector.HasChanged();
            if (changed)
            {
                reflectionCamMover.Move();
                reflectionDataUpdater.Update();
                renderController.MarkDirty();
            }
        }
    }
}
using System;

namespace Snm.Runtime.WaterSystem
{
    public class MirroringCameraUpdater : IUpdateTarget
    {
        private readonly TransformChangeDetector targetCamMoveDetector;
        private readonly TransformMirroringMover mirrorCamMover;
        private readonly ReflectionMatrixDataUpdater reflectionDataUpdater;
        private readonly WaterReflectionRenderController renderController;

        public MirroringCameraUpdater(
            TransformChangeDetector targetCamMoveDetector,
            TransformMirroringMover mirrorCamMover,
            ReflectionMatrixDataUpdater reflectionDataUpdater,
            WaterReflectionRenderController renderController)
        {
            this.targetCamMoveDetector = targetCamMoveDetector;
            this.mirrorCamMover = mirrorCamMover;
            this.reflectionDataUpdater = reflectionDataUpdater;
            this.renderController = renderController;
            mirrorCamMover.Move();
            reflectionDataUpdater.Update();
        }

        public void Update()
        {
            var changed = targetCamMoveDetector.HasChanged();
            if (changed)
            {
                mirrorCamMover.Move();
                reflectionDataUpdater.Update();
                renderController.MarkDirty();
            }
        }
    }
}
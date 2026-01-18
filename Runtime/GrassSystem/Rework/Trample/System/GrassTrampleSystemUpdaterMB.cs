using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [ExecuteInEditMode]
    public class GrassTrampleSystemUpdaterMB : MonoBehaviour
    {
        private GrassTrampleRenderer _renderer;
        private BrushRenderBatchesMaker _brushBatchMaker;
        private GrassTrampleBrushDirUpdater _driver;

        public void SetBrushDirUpdater(GrassTrampleBrushDirUpdater driver) => _driver = driver;
        public void SetBrushBatchMaker(BrushRenderBatchesMaker brushBatchMaker) => _brushBatchMaker = brushBatchMaker;
        public void SetRenderer(GrassTrampleRenderer renderer) => _renderer = renderer;

        private void Update()
        {
            _driver?.Update();
            _brushBatchMaker?.Update();
            _renderer?.Render(Time.deltaTime);
        }
    }
}
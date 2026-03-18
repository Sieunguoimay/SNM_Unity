using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [ExecuteInEditMode]
    public class GrassTrampleSystemUpdaterMB : MonoBehaviour
    {
        private GrassTrampleRenderer _renderer;
        private GrassTrampleBrushRegistry _brushRegistry;
        private GrassTrampleBrushDirUpdater _driver;

        public void Init(
            GrassTrampleBrushDirUpdater driver,
            GrassTrampleBrushRegistry brushRegistry,
            GrassTrampleRenderer renderer)
        {
            _driver = driver;
            _brushRegistry = brushRegistry;
            _renderer = renderer;
        }

        private void Update()
        {
            _driver?.Update();
            _renderer?.FillStamps(_brushRegistry?.GetBrushes());
            _renderer?.Render(Time.deltaTime);
        }
    }
}

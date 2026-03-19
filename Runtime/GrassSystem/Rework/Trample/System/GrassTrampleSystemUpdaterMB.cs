using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [ExecuteInEditMode]
    public class GrassTrampleSystemUpdaterMB : MonoBehaviour
    {
        private GrassTrampleSystemConfig _config;
        private GrassDisturberTracker _tracker;
        private GrassTrampleRenderer _renderer;

        public void Init(
            GrassTrampleSystemConfig config,
            GrassDisturberTracker tracker,
            GrassTrampleRenderer renderer)
        {
            _config = config;
            _tracker = tracker;
            _renderer = renderer;
        }

        private void Update()
        {
            if (_config == null || !_config.enabled) return;

            _tracker.Update(_renderer.StampBuffer);
            _renderer.Render(Time.deltaTime);
        }
    }
}

using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleRendererMB : MonoBehaviour
    {
        private GrassTrampleRenderer _renderer;

        public void SetRenderer(GrassTrampleRenderer renderer) => _renderer = renderer;

        private void Update()
        {
            _renderer?.Render(Time.deltaTime);
        }
    }
}
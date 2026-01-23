using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [ExecuteInEditMode]
    public class GrassFieldRendererMB : MonoBehaviour
    {
        private GrassFieldRenderer _renderer;

        public void SetRenderer(GrassFieldRenderer renderer) => _renderer = renderer;

        private void Update()
        {
            _renderer?.Render();
        }
    }
}
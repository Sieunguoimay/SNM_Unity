#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class WaterSystemEntrypointMB : MonoBehaviour
    {
        [SerializeField] private WaterSystemConfig config;

        private WaterSystemHandle _handle;
#if UNITY_EDITOR
        private RenderTexturePreviewWindow _window;
#endif
        private void Start()
        {
            Setup();
        }

        private void OnDestroy()
        {
            Teardown();
        }

        [ContextMenu("Setup")]
        private void Setup()
        {
            if (!isActiveAndEnabled) return;
            if (config.waterSurfaceShader == null && config.waterSurfaceMaterial == null) return;

            _handle = WaterSystemInstaller.Install(gameObject, config, Camera.main);

#if UNITY_EDITOR
            _handle.PreviewReflectionTexture.PreviewReflectionTextureUpdated += OnPreviewRenderTextureUpdated;

            if (_window != null)
            {
                _window.SetRenderTexture(_handle.PreviewReflectionTexture.RenderTexture);
            }
#endif
        }

        [ContextMenu("Teardown")]
        private void Teardown()
        {
            if (_handle != null)
            {
                _handle.Destroyer.Dispose();
                _handle = null;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Open Preview RenderTexture")]
        private void OpenPreviewRenderTexture()
        {
            if (_handle != null)
            {
                _window = EditorWindow.GetWindow<RenderTexturePreviewWindow>();
                _window.SetRenderTexture(_handle.PreviewReflectionTexture.RenderTexture);
            }
        }

        private void OnPreviewRenderTextureUpdated()
        {
            if (_window != null)
            {
                _window.Repaint();
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(config.waterSurfaceSize.x, 0, config.waterSurfaceSize.y));
        }
#endif
    }
}
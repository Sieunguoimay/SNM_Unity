using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [ExecuteInEditMode]
    public class GrassFieldMB : MonoBehaviour
    {
        [SerializeField] private Mesh mesh;
        [SerializeField] private Vector3 meshScale = new(1, 1, 1);
        [SerializeField] private Material material;
        [SerializeField] private Transform interactor;
        [SerializeField] private Mesh ground;
        [SerializeField] private WindConfig windData;

        private GrassFieldRenderer _grassFieldRenderer;
        private System.IDisposable _traceSystemDisposable;
        private System.Action _openRTPreviewWindowAction;
        private Transform _painterTransform;
        private float _interactorRadius;

        private void OnEnable()
        {
            _interactorRadius = interactor.localScale.x;
            TryDeleteRenderer();
            TryCreateRenderer();
        }

        private void OnDisable()
        {
            TryDeleteRenderer();
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            TryDeleteRenderer();
            TryCreateRenderer();
        }

        private void TryDeleteRenderer()
        {
            DeleteTraceSystem();
            _grassFieldRenderer?.Cleanup();
            _grassFieldRenderer = null;
        }

        private void TryCreateRenderer()
        {
            if (mesh == null || material == null) return;

            CreateTraceSystem(out var trampleRT, out var worldCanvas);

            var grassMatrices = new GrassWorldMatricesProvider_FromMesh(ground).GetWorldMatrices(meshScale, transform.localToWorldMatrix);

            _grassFieldRenderer = new GrassFieldRenderer(mesh, material);
            _grassFieldRenderer.SetMatrices(grassMatrices);
            _grassFieldRenderer.SetWorldCanvas(worldCanvas);
            _grassFieldRenderer.SetTrampleConfig(trampleRT);
            _grassFieldRenderer.SetWindConfig(windData);
        }

        private void CreateTraceSystem(out RenderTexture trampleRT, out WorldCanvas worldCanvas)
        {
            _traceSystemDisposable = new InteractorTraceSystemInstaller().Install(
                _interactorRadius,
                out _openRTPreviewWindowAction,
                out trampleRT,
                out _painterTransform,
                out worldCanvas);
            _painterTransform.SetParent(interactor);
            _painterTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void DeleteTraceSystem()
        {
            if (_painterTransform != null)
            {
                _painterTransform.SetParent(null);
                _painterTransform = null;
            }

            _traceSystemDisposable?.Dispose();
            _openRTPreviewWindowAction = null;
        }

        private void LateUpdate()
        {
            _grassFieldRenderer?.Render();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(interactor.position, interactor.localScale.x);
        }

        [ContextMenu("Validate")]
        private void Validate()
        {
            OnValidate();
        }

        [ContextMenu("Open TrampleRT Preview Window")]
        private void OpenTrampleRTPreviewWindow()
        {
            _openRTPreviewWindowAction?.Invoke();
        }
#endif
    }
}
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassWorldMatricesProvider_FromMesh
    {
        private readonly Mesh mesh;

        public GrassWorldMatricesProvider_FromMesh(Mesh mesh)
        {
            this.mesh = mesh;
        }

        public Matrix4x4[] GetWorldMatrices(Vector3 scale, Matrix4x4 localToWorld)
        {
            if (mesh == null)
                return System.Array.Empty<Matrix4x4>();

            var vertices = mesh.vertices;
            var normals = mesh.normals;

            return GetWorldMatrices(vertices, normals, scale, localToWorld);
        }

        public static Matrix4x4[] GetWorldMatrices(Vector3[] vertices, Vector3[] normals, Vector3 scale, Matrix4x4 localToWorld)
        {
            var count = vertices.Length;
            var matrices = new Matrix4x4[count];

            for (int i = 0; i < count; i++)
            {
                Vector3 position = vertices[i];

                // Fallback if mesh has no normals
                Vector3 normal = (normals != null && normals.Length == count)
                    ? normals[i]
                    : Vector3.up;

                // Rotate grass so its up-axis follows the surface normal
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);

                matrices[i] = localToWorld * Matrix4x4.TRS(
                    position,
                    rotation,
                    scale
                );
            }

            return matrices;
        }
    }

    [ExecuteInEditMode]
    public class GrassFieldMB : MonoBehaviour
    {
        [SerializeField] private Mesh mesh;
        [SerializeField] private Vector3 meshScale = new(1, 1, 1);
        [SerializeField] private Material material;
        [SerializeField] private Transform interactor;
        [SerializeField] private Mesh ground;

        private GrassFieldRenderer _grassFieldRenderer;
        private System.IDisposable _traceSystemDisposable;
        private System.Action _openRTPreviewWindowAction;
        private Transform _painterTransform;

        private void OnEnable()
        {
            TryDeleteRenderer();
            TryCreateRenderer();
        }

        private void OnDisable()
        {
            TryDeleteRenderer();
        }

        private void OnValidate()
        {
            TryDeleteRenderer();
            TryCreateRenderer();
        }

        private void TryDeleteRenderer()
        {
            DeleteTraceSystem();
            _grassFieldRenderer = null;
        }

        private void TryCreateRenderer()
        {
            if (mesh == null || material == null) return;

            CreateTraceSystem(out var trampleRT, out var worldCanvas);

            var grassMatrices = new GrassWorldMatricesProvider_FromMesh(ground).GetWorldMatrices(meshScale, transform.localToWorldMatrix);

            _grassFieldRenderer = new GrassFieldRenderer(mesh, material);
            _grassFieldRenderer.SetMatrices(grassMatrices);
            _grassFieldRenderer.SetupSway(grassMatrices.Length);
            _grassFieldRenderer.SetTrampleRT(trampleRT, worldCanvas);
        }

        private void CreateTraceSystem(out RenderTexture trampleRT, out WorldCanvas worldCanvas)
        {
            _traceSystemDisposable = new InteractorTraceSystemInstaller().Install(out _openRTPreviewWindowAction, out trampleRT, out _painterTransform, out worldCanvas);
            // _painterTransform.SetParent(interactor);
        }

        private void DeleteTraceSystem()
        {
            if (_painterTransform != null)
            {
                // _painterTransform.SetParent(null);
                _painterTransform = null;
            }

            _traceSystemDisposable?.Dispose();
            _openRTPreviewWindowAction = null;
        }

        private void LateUpdate()
        {
            if (interactor != null)
            {
                _grassFieldRenderer?.SetInteractor(interactor.position, interactor.localScale.x);
            }
            _grassFieldRenderer?.Render();
        }

#if UNITY_EDITOR
        [ContextMenu("Capture All Children")]
        private void CaptureAllChildren()
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

    public class GrassFieldRenderer
    {
        private readonly Mesh mesh;
        private readonly Material material;
        private readonly MaterialPropertyBlock mpb = new();
        private Matrix4x4[] _matrices;

        public GrassFieldRenderer(Mesh mesh, Material material)
        {
            this.mesh = mesh;
            this.material = material;
        }

        public void SetMatrices(Matrix4x4[] matrices)
        {
            _matrices = matrices;
        }

        public void SetInteractor(Vector3 position, float radius)
        {
            material.SetVector("_InteractorPosAndRadius", new Vector4(position.x, position.y, position.z, radius));
        }

        public void SetTrampleRT(RenderTexture trampleRT, WorldCanvas worldCanvas)
        {
            var worldPos = worldCanvas.worldMin;
            var size = worldCanvas.worldMax - worldCanvas.worldMin;

            material.SetTexture("_TrampleRT", trampleRT);
            material.SetVector("_TrampleRect", new Vector4(worldPos.x, worldPos.y, size.x, size.y));
        }

        public void SetupSway(int count)
        {
            var randoms = new Vector4[count];

            for (int i = 0; i < count; i++)
            {
                randoms[i] = new Vector4(
                    Random.value,   // phase
                    Random.value,
                    Random.value,
                    Random.Range(0.5f, 1.2f) // stiffness
                );
            }

            mpb.SetVectorArray("_Random", randoms);
        }

        public void Render()
        {
            if (_matrices == null) return;

            Graphics.DrawMeshInstanced(mesh, 0, material, _matrices, _matrices.Length, mpb);
        }
    }
}
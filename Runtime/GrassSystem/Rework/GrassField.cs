using Snm.Visual.Layout3D;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [ExecuteInEditMode]
    public class GrassField : MonoBehaviour
    {
        [SerializeField] private GridLayoutMB gridLayout;

        private GrassFieldRenderer _renderer;

        public Vector2Int Size => new(gridLayout.GridSize.x, gridLayout.GridSize.z);
        public Vector2 Spacing => new(gridLayout.CellSize.x, gridLayout.CellSize.z);

        public Matrix4x4[] GetGrassWorldMatrices()
        {
            var sizeX = gridLayout.GridSize.x;
            var sizeZ = gridLayout.GridSize.z;

            var spacingX = gridLayout.CellSize.x;
            var spacingZ = gridLayout.CellSize.z;

            var count = sizeX * sizeZ;
            var matrices = new Matrix4x4[count];

            var index = 0;

            var originOffset = new Vector3(
                -(sizeX - 1) * spacingX * 0.5f,
                0f,
                -(sizeZ - 1) * spacingZ * 0.5f
            );

            for (var z = 0; z < sizeZ; z++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    var localPos = new Vector3(
                        x * spacingX,
                        0f,
                        z * spacingZ
                    ) + originOffset;

                    var worldPos = transform.TransformPoint(localPos);

                    matrices[index++] = Matrix4x4.TRS(
                        worldPos,
                        transform.rotation,
                        Vector3.one
                    );
                }
            }

            return matrices;
        }

        public void SetRenderer(GrassFieldRenderer renderer) { _renderer = renderer; }

        private void Update()
        {
            _renderer?.Render();
        }
    }
}
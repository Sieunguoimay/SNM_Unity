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
            var pivotOffset = gridLayout.GetPivotOffset();

            for (var z = 0; z < sizeZ; z++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    var localPos = new Vector3(
                        x * spacingX,
                        0f,
                        z * spacingZ
                    ) + pivotOffset;

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

        public WorldCanvas GetWorldCanvas()
        {
            var sizeX = gridLayout.GridSize.x;
            var sizeZ = gridLayout.GridSize.z;

            var spacingX = gridLayout.CellSize.x;
            var spacingZ = gridLayout.CellSize.z;

            // Local size of the grid
            var localSize = new Vector2(
                (sizeX - 1) * spacingX,
                (sizeZ - 1) * spacingZ
            );

            // Local pivot offset (XZ plane)
            var localMin3D = gridLayout.GetPivotOffset();
            var localMax3D = localMin3D + new Vector3(localSize.x, 0f, localSize.y);

            // Convert to world space
            var worldMin3D = transform.TransformPoint(localMin3D);
            var worldMax3D = transform.TransformPoint(localMax3D);

            return new WorldCanvas
            {
                worldMin = new Vector2(worldMin3D.x, worldMin3D.z),
                worldMax = new Vector2(worldMax3D.x, worldMax3D.z),
            };
        }

        public void SetRenderer(GrassFieldRenderer renderer) { _renderer = renderer; }

        private void Update()
        {
            _renderer?.Render();
        }
    }
}
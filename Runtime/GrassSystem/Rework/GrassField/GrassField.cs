using Snm.SurfaceInteraction;
using Snm.Visual.Layout3D;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassField : MonoBehaviour
    {
        [SerializeField] private GridLayoutMB gridLayout;

        public Vector2Int Dimension => new(gridLayout.GridSize.x, gridLayout.GridSize.z);
        public Vector2 Spacing => new(gridLayout.CellSize.x, gridLayout.CellSize.z);

        private void OnDrawGizmos()
        {
            var canvas = GetSurfaceCanvas();
            var min = canvas.WorldMin;
            var max = canvas.WorldMax;
            var size = max - min;
            var center = (max + min) / 2f;

            var old = Gizmos.color;
            Gizmos.color = Color.green;

            Gizmos.DrawWireCube(new Vector3(center.x, 0, center.y), new Vector3(size.x, 0, size.y));

            Gizmos.color = old;
        }

        public Matrix4x4[] GetGrassMatrices()
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

        public SurfaceCanvas GetSurfaceCanvas()
        {
            var sizeX = gridLayout.GridSize.x;
            var sizeZ = gridLayout.GridSize.z;

            var spacingX = gridLayout.CellSize.x;
            var spacingZ = gridLayout.CellSize.z;

            var localSize = new Vector2(
                (sizeX - 1) * spacingX,
                (sizeZ - 1) * spacingZ
            );

            var localMin3D = gridLayout.GetPivotOffset();
            var localMax3D = localMin3D + new Vector3(localSize.x, 0f, localSize.y);

            var worldMin3D = transform.TransformPoint(localMin3D);
            var worldMax3D = transform.TransformPoint(localMax3D);

            var center = (worldMin3D + worldMax3D) * 0.5f;
            var worldSize = worldMax3D - worldMin3D;

            return new SurfaceCanvas
            {
                Position = center,
                Rotation = Quaternion.identity,
                Size = new Vector2(worldSize.x, worldSize.z)
            };
        }

        public Bounds GetWorldBounds(float grassHeight, float windAmplitude)
        {
            var sizeX = gridLayout.GridSize.x;
            var sizeZ = gridLayout.GridSize.z;

            var spacingX = gridLayout.CellSize.x;
            var spacingZ = gridLayout.CellSize.z;

            // Local grid size
            float width = (sizeX - 1) * spacingX;
            float depth = (sizeZ - 1) * spacingZ;

            // Vertical range
            float height = grassHeight + windAmplitude;

            // Local center
            Vector3 localCenter = gridLayout.GetPivotOffset()
                + new Vector3(width * 0.5f, height * 0.5f, depth * 0.5f);

            Vector3 localSize = new Vector3(width, height, depth);

            // Convert to world space (handles rotation correctly)
            return TransformBounds(transform.localToWorldMatrix, new Bounds(localCenter, localSize));
        }

        public static Bounds TransformBounds(Matrix4x4 m, Bounds b)
        {
            var center = m.MultiplyPoint3x4(b.center);

            Vector3 extents = b.extents;

            Vector3 axisX = m.MultiplyVector(new Vector3(extents.x, 0, 0));
            Vector3 axisY = m.MultiplyVector(new Vector3(0, extents.y, 0));
            Vector3 axisZ = m.MultiplyVector(new Vector3(0, 0, extents.z));

            Vector3 worldExtents = new Vector3(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z)
            );

            return new Bounds(center, worldExtents * 2);
        }
    }
}
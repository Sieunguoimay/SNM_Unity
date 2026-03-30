#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// Spatial acceleration structure for fast "vertices within sphere" queries.
    /// Divides the mesh bounding box into a flat grid (3D) where each cell stores
    /// a list of vertex indices. Queries find all overlapping cells and distance-check
    /// the vertices within them.
    /// </summary>
    public class MeshQueryAccel
    {
        private Vector3[] _positions;
        private Vector3 _boundsMin;
        private float _cellSize;
        private int _gridX, _gridY, _gridZ;
        private List<int>[] _cells;

        /// <summary>
        /// Builds the spatial grid from the given vertex positions.
        /// </summary>
        /// <param name="positions">Vertex positions array.</param>
        /// <param name="cellSize">Grid cell size. A good default is 2x the typical brush radius.</param>
        public void Build(Vector3[] positions, float cellSize)
        {
            _positions = positions;
            _cellSize = Mathf.Max(cellSize, 0.001f);

            if (positions == null || positions.Length == 0)
            {
                _cells = null;
                return;
            }

            // Compute bounds
            var min = positions[0];
            var max = positions[0];
            for (int i = 1; i < positions.Length; i++)
            {
                min = Vector3.Min(min, positions[i]);
                max = Vector3.Max(max, positions[i]);
            }

            // Expand slightly to avoid edge cases
            min -= Vector3.one * _cellSize * 0.01f;
            max += Vector3.one * _cellSize * 0.01f;
            _boundsMin = min;

            var size = max - min;
            const int maxGridDim = 256; // Cap to prevent excessive memory usage
            _gridX = Mathf.Clamp(Mathf.CeilToInt(size.x / _cellSize), 1, maxGridDim);
            _gridY = Mathf.Clamp(Mathf.CeilToInt(size.y / _cellSize), 1, maxGridDim);
            _gridZ = Mathf.Clamp(Mathf.CeilToInt(size.z / _cellSize), 1, maxGridDim);

            int totalCells = _gridX * _gridY * _gridZ;
            _cells = new List<int>[totalCells];

            // Populate cells
            for (int i = 0; i < positions.Length; i++)
            {
                int cellIdx = GetCellIndex(positions[i]);
                if (cellIdx < 0 || cellIdx >= totalCells) continue;

                if (_cells[cellIdx] == null)
                    _cells[cellIdx] = new List<int>();
                _cells[cellIdx].Add(i);
            }
        }

        /// <summary>
        /// Returns all vertex indices within the given sphere.
        /// </summary>
        public List<int> GetVerticesInSphere(Vector3 center, float radius)
        {
            var result = new List<int>();
            if (_cells == null || _positions == null) return result;

            float radiusSqr = radius * radius;

            // Find the range of cells overlapping the sphere
            var sphereMin = center - Vector3.one * radius;
            var sphereMax = center + Vector3.one * radius;

            int minCX = Mathf.Max(0, Mathf.FloorToInt((sphereMin.x - _boundsMin.x) / _cellSize));
            int minCY = Mathf.Max(0, Mathf.FloorToInt((sphereMin.y - _boundsMin.y) / _cellSize));
            int minCZ = Mathf.Max(0, Mathf.FloorToInt((sphereMin.z - _boundsMin.z) / _cellSize));
            int maxCX = Mathf.Min(_gridX - 1, Mathf.FloorToInt((sphereMax.x - _boundsMin.x) / _cellSize));
            int maxCY = Mathf.Min(_gridY - 1, Mathf.FloorToInt((sphereMax.y - _boundsMin.y) / _cellSize));
            int maxCZ = Mathf.Min(_gridZ - 1, Mathf.FloorToInt((sphereMax.z - _boundsMin.z) / _cellSize));

            for (int cx = minCX; cx <= maxCX; cx++)
            {
                for (int cy = minCY; cy <= maxCY; cy++)
                {
                    for (int cz = minCZ; cz <= maxCZ; cz++)
                    {
                        int cellIdx = cx + cy * _gridX + cz * _gridX * _gridY;
                        var cell = _cells[cellIdx];
                        if (cell == null) continue;

                        for (int i = 0; i < cell.Count; i++)
                        {
                            int vi = cell[i];
                            float distSqr = (_positions[vi] - center).sqrMagnitude;
                            if (distSqr <= radiusSqr)
                                result.Add(vi);
                        }
                    }
                }
            }

            return result;
        }

        private int GetCellIndex(Vector3 position)
        {
            int cx = Mathf.FloorToInt((position.x - _boundsMin.x) / _cellSize);
            int cy = Mathf.FloorToInt((position.y - _boundsMin.y) / _cellSize);
            int cz = Mathf.FloorToInt((position.z - _boundsMin.z) / _cellSize);

            cx = Mathf.Clamp(cx, 0, _gridX - 1);
            cy = Mathf.Clamp(cy, 0, _gridY - 1);
            cz = Mathf.Clamp(cz, 0, _gridZ - 1);

            return cx + cy * _gridX + cz * _gridX * _gridY;
        }
    }
}
#endif

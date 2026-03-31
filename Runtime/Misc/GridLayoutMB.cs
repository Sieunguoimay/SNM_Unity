using Snm.Components;
using UnityEngine;

namespace Snm.Visual.Layout3D
{
    public class GridLayoutMB : MonoBehaviour
    {
        [SerializeField] private Vector3Int gridSize;
        [SerializeField] private Vector3 cellSize;
        [SerializeField] private PivotType pivot;

        public Vector3Int GridSize => gridSize;
        public Vector3 CellSize => cellSize;
        public int CellCount => gridSize.x * gridSize.y * gridSize.z;

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        public Vector3 GetRandomPosInArea()
        {
            return transform.TransformPoint(GetCellPositionByCellIndex(UnityEngine.Random.Range(0, CellCount)));
        }

        public Vector3 GetCellPositionByCellIndex(int index)
        {
            var offset = GetPivotOffset();
            var x = index % gridSize.x;
            var z = index / gridSize.x % gridSize.z;
            var y = index / gridSize.x / gridSize.z;
            return offset + Vector3.Scale(cellSize, new Vector3(x, y, z));
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var offset = GetPivotOffset();
            Gizmos.matrix = transform.localToWorldMatrix;
            for (var i = 0; i < gridSize.x; i++)
            {
                for (var j = 0; j < gridSize.y; j++)
                {
                    for (var k = 0; k < gridSize.z; k++)
                    {
                        Gizmos.DrawWireCube(offset + Vector3.Scale(cellSize, new Vector3(i, j, k)), Vector3.Scale(cellSize, Vector3.one));
                    }
                }
            }
        }
#endif

        public Vector3 GetPivotOffset()
        {
            if (pivot == PivotType.FirstCell)
            {
                return Vector3.zero;
            }
            else if (pivot == PivotType.GridCenter)
            {
                return -.5f * (Vector3.Scale(cellSize, gridSize) - cellSize);
            }
            else
            {
                return .5f * cellSize;
            }
        }

        public enum PivotType
        {
            FirstCell,
            GridCenter,
            GridBottomLeft,
        }

        [ContextMenu("OnTransformChildrenChanged")]
        private void OnTransformChildrenChanged()
        {
            if (gameObject.activeInHierarchy)
            {
                if (Application.IsPlaying(this))
                {
                    this.ExecuteInNextFrame(() =>
                    {
                        for (var i = 0; i < transform.childCount; i++)
                        {
                            transform.GetChild(i).localPosition = GetCellPositionByCellIndex(i);
                        }
                    });
                }
                else
                {
                    for (var i = 0; i < transform.childCount; i++)
                    {
                        transform.GetChild(i).localPosition = GetCellPositionByCellIndex(i);
                    }
                }
            }
        }
    }
}

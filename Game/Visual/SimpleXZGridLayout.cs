using UnityEngine;
using UnityEngine.Serialization;

public class SimpleXZGridLayout : MonoBehaviour
{
    [FormerlySerializedAs("gridSize")]
    [SerializeField] private Vector2 cellSize;
    [SerializeField] private int cellsPerRow;

    private void OnEnable()
    {
        this.ExecuteInNextFrame(() =>
        {
            UpdateLayout();
        });
    }

    [ContextMenu("UpdateLayout")]
    public void UpdateLayout()
    {
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            var pos = child.localPosition;
            pos.x = cellSize.x * (i % cellsPerRow);
            pos.z = cellSize.y * (i / cellsPerRow);
            child.localPosition = pos;
        }
    }
}
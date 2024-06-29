using UnityEngine;

public class CustomGridLayout : MonoBehaviour
{
    [SerializeField] private float spacing = 0f;
    [SerializeField] private Vector2 cellSize;
    [SerializeField] private Transform container;

    public void RefreshLayout()
    {
        //this.ExecuteInNextFrame(() =>
        //{
        //    var index = 0;
        //    foreach (Transform child in container)
        //    {
        //        var pos = child.localPosition;
        //        pos.x = index % col * cellSize.x;
        //        pos.z = Mathf.Floor(index / col) * cellSize.y;
        //        child.localPosition = pos;
        //        index++;
        //    }
        //});
    }
}
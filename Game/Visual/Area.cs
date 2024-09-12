using UnityEngine;

public class Area : MonoBehaviour
{
    [SerializeField] private Bounds bounds;
    [SerializeField] private Color gizmosColor = Color.white;

    public Bounds LocalBounds => bounds;

    public Vector3 GetRandomPosInArea()
    {
        var x = Random.Range(bounds.min.x, bounds.max.x);
        var y = Random.Range(bounds.min.y, bounds.max.y);
        var z = Random.Range(bounds.min.z, bounds.max.z);
        return transform.TransformPoint(new Vector3(x, y, z));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmosColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}

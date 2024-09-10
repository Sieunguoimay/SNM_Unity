using UnityEngine;
using UnityEngine.Events;

public class ClickBox : MonoBehaviour
{
    [SerializeField] private Vector3 boxSize = Vector3.one;
    [SerializeField] private UnityEvent onClick;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var localRay = TransformRayToLocal(ray);
            var bounds = new Bounds(Vector3.zero, boxSize);
            if (bounds.IntersectRay(localRay))
            {
                onClick.Invoke();
            }
        }
    }
    
    Ray TransformRayToLocal(Ray worldRay)
    {
        // Get the inverse transformation matrix of the object
        Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;
        Matrix4x4 worldToLocalMatrix = localToWorldMatrix.inverse;

        // Transform the ray to local space
        Vector3 localOrigin = worldToLocalMatrix.MultiplyPoint(worldRay.origin);
        Vector3 localDirection = worldToLocalMatrix.MultiplyVector(worldRay.direction);

        return new Ray(localOrigin, localDirection);
    }
    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
    }
}
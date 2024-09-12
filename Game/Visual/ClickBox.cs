using System;
using UnityEngine;
using UnityEngine.Events;

public class ClickBox : MonoBehaviour
{
    [SerializeField] private Vector3 boxSize = Vector3.one;
    [SerializeField] private UnityEvent onClick;

    public event Action<ClickBox, Vector3> OnClicked;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var localRay = TransformRayToLocal(ray, transform.worldToLocalMatrix);
            var bounds = new Bounds(Vector3.zero, boxSize);
            if (bounds.IntersectRay(localRay, out var distance))
            {
                onClick?.Invoke();
                OnClicked?.Invoke(this, transform.TransformPoint(localRay.origin + localRay.direction * distance));
            }
        }
    }

    private static Ray TransformRayToLocal(Ray worldRay, Matrix4x4 worldToLocalMatrix)
    {
        return new Ray(
            worldToLocalMatrix.MultiplyPoint(worldRay.origin),
            worldToLocalMatrix.MultiplyVector(worldRay.direction)
        );
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
    }

}
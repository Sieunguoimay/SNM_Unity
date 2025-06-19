using System;
using Snm.Layout;
using UnityEngine;
using UnityEngine.Events;

public class ClickBox : MonoBehaviour, IWorldBounds
{
    [SerializeField] private Vector3 boxSize = Vector3.one;
    [SerializeField] private UnityEvent onClick;

    public Vector3 BoxSize => boxSize;

    Bounds IWorldBounds.Bounds => new(Vector3.zero, boxSize);
    Transform IWorldBounds.Transform => transform;

    public event Action<ClickBox, Vector3> OnClicked;

    private void OnEnable()
    {
        ClickBoxService.Instance.RegisterClickBox(this);
    }

    private void OnDisable()
    {
        ClickBoxService.Instance?.UnregisterClickBox(this);
    }

    public void HandleClicked(Vector3 position)
    {
        onClick?.Invoke();
        OnClicked?.Invoke(this, position);
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
    }
}

using System;
using Game;
using SNM.Layout;
using UnityEngine;
using UnityEngine.Events;

public class ClickBox : MonoBehaviour, ITouchBehaviourTarget, ITouchTargetFilterWithBounds, ITouchListenerClick, IWorldBounds
{
    [SerializeField] private Vector3 boxSize = Vector3.one;
    [SerializeField] private UnityEvent onClick;

    public Vector3 BoxSize => boxSize;

    ITouchTargetFilter ITouchBehaviourTarget.TouchTargetFilter => this;
    ITouchListener ITouchBehaviourTarget.TouchListener => this;
    IWorldBounds ITouchTargetFilterWithBounds.WorldBounds => this;

    Bounds IWorldBounds.Bounds => new(Vector3.zero, boxSize);
    Transform IWorldBounds.Transform => transform;

    public event Action<ClickBox, Vector3> OnClicked;

    private void OnEnable()
    {
        var inputService = GameServiceLocator.Instance.Get<IInputBehaviourService>();
        if (inputService != null)
        {
            inputService.Register(this);
        }
        else
        {
            ClickBoxService.Instance.RegisterClickBox(this);
        }
    }

    private void OnDisable()
    {
        var inputService = GameServiceLocator.Instance.Get<IInputBehaviourService>();
        if (inputService != null)
        {
            inputService.Unregister(this);
        }
        else
        {
            ClickBoxService.Instance?.UnregisterClickBox(this);
        }
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

    void ITouchListenerClick.OnClickBegin()
    {

    }

    void ITouchListenerClick.OnClickEnd()
    {
        HandleClicked(Vector3.zero);
    }
}

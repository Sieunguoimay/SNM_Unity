using System.Collections.Generic;
using System.Linq;
using EventBus;
using UnityEngine;
using UnityEngine.Events;

public class AnimationEventFilter : MonoBehaviour, IEventHandler
{
    [SerializeField] private EventChannel_Animation eventChannel;
    [SerializeField] private GameObject animationClipSource;
    [StringSelector(nameof(EventParameters))]
    [SerializeField] private string parameter;
    [SerializeField] private UnityEvent onTrigger;

    public IEnumerable<string> EventParameters
    {
        get
        {
#if UNITY_EDITOR
            return animationClipSource != null
                    ? UnityEditor.AnimationUtility.GetAnimationClips(animationClipSource).SelectMany(c => c.events.Select(e => e.stringParameter))
                    : Enumerable.Empty<string>();
#else
            return Enumerable.Empty<string>();
#endif
        }

    }

    private void Start()
    {
        eventChannel.AddHandler(this);
    }

    private void OnDestroy()
    {
        eventChannel.RemoveHandler(this);
    }

    public void FilterEvent(string eventParam)
    {
        if (eventParam == parameter)
        {
            onTrigger.Invoke();
        }
    }

    public void OnReceivedEvent(IEventObject eventObject, IEventSender trigger, object data)
    {
        if (eventObject == EventChannel_Animation.AnimationEventObject)
        {
            FilterEvent((string)data);
        }
    }
}
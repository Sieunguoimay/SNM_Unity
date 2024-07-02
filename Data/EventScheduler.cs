using System;
using System.Collections;
using Reflection;
using UnityEngine;

public class EventScheduler
{
    [InjectField] private readonly Action eventAction;
    private Coroutine _coroutine;

    public bool IsRunning { get; private set; }
    public float IntervalSeconds { get; private set; }

    public event Action<EventScheduler> EventScheduled;

    public EventScheduler()
    {
        IsRunning = false;
    }

    public EventScheduler(Action eventAction)
    {
        this.eventAction = eventAction;
        IsRunning = false;
    }

    public void Start()
    {
        if (_coroutine != null)
        {
            PublicMonoBehaviour.Instance.StopCoroutine(_coroutine);
        }

        IsRunning = true;
        _coroutine = PublicMonoBehaviour.Instance.StartCoroutine(EventLoop());
    }

    public void Stop()
    {
        IsRunning = false;
        _coroutine = null;
    }

    public void SetIntervalSecs(float secs)
    {
        IntervalSeconds = secs;
    }

    private IEnumerator EventLoop()
    {
        while (IsRunning)
        {
            yield return new WaitForSeconds(IntervalSeconds);
            eventAction?.Invoke();
            EventScheduled?.Invoke(this);
        }
    }
}

using GameNode;
using InspectorExtensions;
using System;
using UnityEngine;

public class Timer : NodeSO
{
    [SerializeField] private float totalTime = 100;
    [SerializeField] private float interval = .25f;

    private EventScheduler _clockEventScheduler;

    [NonSerialized]
    [RevealNonSerialized]
    private float _time;

    [RevealNonSerialized]
    public float TimeFactor => _time / totalTime;

    public event Action<Timer> TimeChangedEvent;

    public override void Setup()
    {
        base.Setup();
        _clockEventScheduler = new EventScheduler(OnClockEventTriggered);
        _clockEventScheduler.SetIntervalSecs(interval);
        _clockEventScheduler.Start();
    }

    public override void TearDown()
    {
        _clockEventScheduler.Stop();
        base.TearDown();
    }

    private void OnClockEventTriggered()
    {
        UpdateIntervals();
    }

    private void UpdateIntervals()
    {
        _time += interval;
        TimeChangedEvent?.Invoke(this);
    }
}


using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class FiniteTimePlayer : MonoBehaviour
{
    [SerializeField] private float selfDuration = 1f;
    [SerializeField] private OutputConfig outputConfig;
    [Tooltip("TimeFactor")]
    [SerializeField] private UnityEventFloat onTick;

    private Coroutine _coroutine;

    public bool IsRunning => _coroutine != null;

    public event Action<FiniteTimePlayer, float> TickEvent;
    public event Action<FiniteTimePlayer> PlayingStatusChangedEvent;

    public void PlayWithSelfDuration()
    {
        Play(selfDuration);
    }

    public void Play(float duration)
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }

        _coroutine = StartCoroutine(Timing(duration));

        PlayingStatusChangedEvent?.Invoke(this);
    }

    public void Stop()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;

            PlayingStatusChangedEvent?.Invoke(this);
        }
    }

    private IEnumerator Timing(float duration)
    {
        var time = 0f;
        while (true)
        {
            var output = outputConfig.ComputeOutput(duration, time);

            onTick?.Invoke(output);
            TickEvent?.Invoke(this, output);

            if (time >= duration)
            {
                break;
            }

            yield return null;

            time += Time.deltaTime;
        }
        
        _coroutine = null;
        PlayingStatusChangedEvent?.Invoke(this);
    }

    [Serializable]
    private class UnityEventFloat : UnityEvent<float> { }

    private enum OutputFormat
    {
        Time,
        Progress,
        Range,
    }

    [Serializable]
    private class OutputConfig
    {
        public OutputFormat outputFormat = OutputFormat.Progress;
        public RangeConfig rangeConfig;

        public float ComputeOutput(float duration, float time)
        {
            if (outputFormat == OutputFormat.Progress)
            {
                return Mathf.Clamp01(time / duration);
            }
            else if (outputFormat == OutputFormat.Time)
            {
                return Mathf.Min(time, duration);
            }
            else if (outputFormat == OutputFormat.Range)
            {
                return Mathf.Lerp(rangeConfig.range.x, rangeConfig.range.y, Mathf.Clamp01(time / duration));
            }

            return time;
        }
    }

    [Serializable]
    private class RangeConfig
    {
        public Vector2 range;
    }
}

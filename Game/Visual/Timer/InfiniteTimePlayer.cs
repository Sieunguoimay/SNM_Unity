using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class InfiniteTimePlayer : MonoBehaviour
{
    [SerializeField]
    [Tooltip("TimeFactor")]
    private UnityEventFloat onFactorValue;

    public event Action<InfiniteTimePlayer, float> OnTick;
    public event Action<InfiniteTimePlayer> PlayingStatusChangedEvent;

    private Coroutine _coroutine;
    private float _currentTime = 0;
    private bool _isPlaying = false;
    private bool _isPaused = false;

    public bool IsPlaying => _isPlaying;
    public bool IsPaused => _isPaused;
    public float CurrentTime => _currentTime;

    public void Play()
    {
        _isPlaying = true;
        StartTimeLoopCoroutine(0);
        PlayingStatusChangedEvent?.Invoke(this);
    }

    public void Pause()
    {
        _isPaused = true;
        StopTimeLoopCoroutine();
        PlayingStatusChangedEvent?.Invoke(this);
    }

    public void Resume()
    {
        StartTimeLoopCoroutine(_currentTime);
        _isPaused = false;
        PlayingStatusChangedEvent?.Invoke(this);
    }

    public void Stop()
    {
        StopTimeLoopCoroutine();
        _isPlaying = false;
        PlayingStatusChangedEvent?.Invoke(this);
    }

    private void StartTimeLoopCoroutine(float startTime)
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }
        _coroutine = StartCoroutine(IE_TimeLoop(startTime));
    }

    private void StopTimeLoopCoroutine()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }

    private IEnumerator IE_TimeLoop(float startTime)
    {
        _currentTime = startTime;

        while (true)
        {
            onFactorValue?.Invoke(_currentTime);
            OnTick?.Invoke(this, _currentTime);

            yield return null;

            _currentTime += Time.deltaTime;
        }
    }

    [Serializable]
    private class UnityEventFloat : UnityEvent<float> { }
}
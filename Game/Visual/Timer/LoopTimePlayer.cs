using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class LoopTimePlayer : MonoBehaviour
{
    [SerializeField]
    [Tooltip("TimeFactor")]
    private UnityEventFloat onFactorValue;

    private Coroutine _coroutine;

    public void Play(float duration)
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }
        _coroutine = StartCoroutine(Timing(duration));
    }

    public void Stop()
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }

    private IEnumerator Timing(float duration)
    {
        var time = 0f;
        while (true)
        {
            onFactorValue?.Invoke(Mathf.Min(1f, time / duration));

            yield return null;

            time += Time.deltaTime;
            if (time > duration)
            {
                time = 0f;
            }
        }
    }

    [Serializable]
    private class UnityEventFloat : UnityEvent<float> { }
}
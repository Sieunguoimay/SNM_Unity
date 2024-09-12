using System;
using System.Collections;
using UnityEngine;

public static class MonoBehaviourExtensions
{
    public static Coroutine ExecuteInNextFrame(this MonoBehaviour mb, Action callback)
    {
        return mb.StartCoroutine(ExecuteInNextFrame(callback));
    }

    private static IEnumerator ExecuteInNextFrame(Action callback)
    {
        yield return null;
        callback?.Invoke();
    }

    public static Coroutine Delay(this MonoBehaviour mb, float duration, Action callback)
    {
        return mb.StartCoroutine(Delay(duration, callback));
    }

    private static IEnumerator Delay(float duration, Action callback)
    {
        yield return new WaitForSeconds(duration);
        callback?.Invoke();
    }

    public static Coroutine StartLerping(this MonoBehaviour mb, float duration, Action<float> onLerp)
    {
        return mb.StartCoroutine(Lerping(duration, onLerp));
    }

    private static IEnumerator Lerping(float duration, Action<float> onLerp)
    {
        var time = 0f;
        while (time < duration)
        {
            onLerp?.Invoke(time / duration);
            yield return null;
            time += Time.deltaTime;
        }
        onLerp?.Invoke(1f);
    }
}
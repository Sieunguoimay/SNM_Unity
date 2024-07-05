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
}
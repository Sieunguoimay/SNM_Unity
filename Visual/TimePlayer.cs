using System;
using System.Collections;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif
using UnityEngine;
using UnityEngine.Events;

public class TimePlayer : MonoBehaviour
{
    [SerializeField] private float selfDuration = 1f;
    [SerializeField] private bool loop = false;
    [SerializeField] private OutputConfig outputConfig;
    [Tooltip("TimeFactor")]
    [SerializeField] private UnityEventFloat onTick;
    private Coroutine _coroutine;
    public bool IsRunning => _coroutine != null;

    public event Action<TimePlayer> EndedEvent;

    [ContextMenu("PlayWithSelfDuration")]
    public void PlayWithSelfDuration()
    {
        Play(selfDuration);
    }

#if UNITY_EDITOR

    private EditorCoroutine _editorCoroutine;

    [ContextMenu("TestPlay")]
    private void TestPlay()
    {
        if (_editorCoroutine != null)
        {
            EditorCoroutineUtility.StopCoroutine(_editorCoroutine);
        }

        if (!Application.isPlaying)
        {
            _editorCoroutine = EditorCoroutineUtility.StartCoroutine(Timing(selfDuration), this);
            return;
        }
    }

#endif

    public void Play(float duration)
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }

        _coroutine = StartCoroutine(Timing(duration));
    }

    private IEnumerator Timing(float duration)
    {
        var time = 0f;
        while (true)
        {
            onTick?.Invoke(outputConfig.ComputeOutput(duration, time));

            if (time >= duration)
            {
                if (loop)
                {
                    time = 0f;
                }
                else
                {
                    break;
                }
            }

            yield return null;

            time += Time.deltaTime;
        }
        _coroutine = null;
        EndedEvent?.Invoke(this);
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

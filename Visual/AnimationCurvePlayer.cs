using InspectorExtensions;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class AnimationCurvePlayer : MonoBehaviour
{
    [SerializeField] private AnimationCurve curve;
    [SerializeField] private UnityEventFloat onEvaluate;
    [SerializeField] private float outputValueMult = 1f;

    private Coroutine _coroutine;

    public float AnimationDurationSeconds => curve.keys[curve.length - 1].time;

    public void PlayEntireAnimationInDuration(float durationSeconds)
    {
        var timeFactor = durationSeconds / AnimationDurationSeconds;
        Play(timeFactor);
    }

    public void Play()
    {
        Play(1f);
    }

    public void Play(float timeFactor)
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }
        _coroutine = StartCoroutine(PlayingCoroutine(timeFactor));
    }

    private IEnumerator PlayingCoroutine(float timeFactor)
    {
        var time = 0f;
        var runningDurationSeconds = AnimationDurationSeconds * timeFactor;

        while (time <= runningDurationSeconds)
        {
            var actualAnimationTime = time / timeFactor;

            EvaluateCurve(actualAnimationTime);

            time += Time.deltaTime;

            yield return null;
        }

        _coroutine = null;
    }

    public void EvaluateByProgress(float progress)
    {
        EvaluateCurve(progress * curve.length);
    }
    
    public void EvaluateCurve(float t)
    {
        var value = curve.Evaluate(t) * outputValueMult;
        onEvaluate?.Invoke(value);
    }

    [System.Serializable]
    private class UnityEventFloat : UnityEvent<float>
    {
    }

#if UNITY_EDITOR

    [ContextMenu("TestPlay")]
    private void TestPlay()
    {
        Play();
    }

    private float _testValue;
    [IMGUIMethod]
    private void OnIMGUI()
    {
        var newValue = EditorGUILayout.Slider(_testValue, 0, 1);
        if (newValue != _testValue)
        {
            _testValue = newValue;
            EvaluateCurve(_testValue);
        }
    }
#endif
}

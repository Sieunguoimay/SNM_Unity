using System;
using Snm.Tools.InspectorExtra;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Snm.Visual.Animation
{

    public class AnimationCurveEvaluator : MonoBehaviour
    {
        [SerializeField] private AnimationCurve curve;
        [SerializeField] private UnityEventFloat onEvaluate;
        [SerializeField] private float outputValueMult = 1f;

        public float AnimationDurationSeconds => curve.keys[curve.length - 1].time;

        public event Action<AnimationCurveEvaluator, float> OnEvaluate;

        public void EvaluateByProgress(float progress)
        {
            Evaluate(progress * curve.length);
        }

        public void Evaluate(float t)
        {
            var value = curve.Evaluate(t) * outputValueMult;
            onEvaluate?.Invoke(value);
            OnEvaluate?.Invoke(this, value);
        }

        [Serializable] private class UnityEventFloat : UnityEvent<float> { }

#if UNITY_EDITOR
        private float _testValue;
        [IMGUIMethod]
        private void OnIMGUI()
        {
            var newValue = EditorGUILayout.Slider(_testValue, 0, 1);
            if (newValue != _testValue)
            {
                _testValue = newValue;
                Evaluate(_testValue);
            }
        }
#endif
    }
}

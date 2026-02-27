using System;
#if UNITY_EDITOR
using Snm.Tools.InspectorExtensions;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

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
        [CreateVisualElement]
        private void CreateToolVE(VisualElement root)
        {
            var testValue = 0f;

            root.Add(new IMGUIContainer(() =>
            {
                var newValue = EditorGUILayout.Slider(testValue, 0, 1);
                if (newValue != testValue)
                {
                    testValue = newValue;
                    Evaluate(testValue);
                }
            }));
        }
#endif
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor.UIElements;
using Snm.Tools.InspectorExtensions;
#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Visual.Animation
{

    public class AnimationClipEvaluator : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private AnimationClipAsset clip;

        [Tooltip("Tick if you want to evaluate out of range [0, length] as circulated instead of clamp")]
        [SerializeField] private bool circulated = false;

        private IAnimationCurveEvaluator[] _runtimeClipCurves;

        public AnimationClipAsset Clip => clip;

        private void Awake()
        {
            _runtimeClipCurves = CreateRuntimeTransformCurves(clip).ToArray();
        }

        public void EvaluateByProgress(float progress)
        {
            Evaluate(progress * clip.ClipLength);
        }

        public void Evaluate(float time)
        {
            if (!Application.isPlaying)
            {
                _runtimeClipCurves ??= CreateRuntimeTransformCurves(clip).ToArray();
            }
            var t = time;
            if (circulated)
            {
                t = time % clip.ClipLength;
                t = t < 0 ? t + clip.ClipLength : t;
            }
            foreach (var c in _runtimeClipCurves)
            {
                c.Evaluate(t);
            }
        }

#if UNITY_EDITOR
        [CreateVisualElement]
        private void OnCreateVisualElement(VisualElement ve)
        {
            if (_runtimeClipCurves != null)
            {
                foreach (var c in _runtimeClipCurves)
                {
                    var line = new VisualElement() { };
                    line.style.flexDirection = FlexDirection.Row;
                    ve.Add(line);
                    if (c is RuntimeClipCurve tr)
                    {
                        var t = new ObjectField() { value = tr.targetTransform };
                        var l = new Label() { text = $"{tr.transformProperty} {tr.vectorProperty}" };
                        var r = new CurveField() { value = tr.animationCurve };
                        r.style.flexGrow = 1;
                        line.Add(t);
                        line.Add(l);
                        line.Add(r);
                    }
                }
            }
        }
#endif

        private IEnumerable<IAnimationCurveEvaluator> CreateRuntimeTransformCurves(AnimationClipAsset clip)
        {
            // var runtimeCurves = new IAnimationCurveEvaluator[clip.ClipCurves.Count];
            for (int i = 0; i < clip.ClipCurves.Count; i++)
            {
                var curveConfig = clip.ClipCurves[i];
                var type = Type.GetType(curveConfig.type);
                if (type == typeof(Transform))
                {
                    var propSegments = curveConfig.propertyName.Split(".");
                    yield return new RuntimeClipCurve()
                    {
                        animationCurve = curveConfig.animationCurve,
                        targetTransform = GetTargetTransfrom(root.transform, curveConfig.path),
                        transformProperty = GetTransformProperty(propSegments[0]),
                        vectorProperty = GetVectorProperty(propSegments[1]),
                    };
                }
            }
        }

        private Transform GetTargetTransfrom(Transform root, string path)
        {
            var pathSegments = path.Split("/");
            var current = root;
            foreach (var s in pathSegments)
            {
                foreach (Transform t in current.transform)
                {
                    if (s == t.name)
                    {
                        current = t;
                        break;
                    }
                }
            }
            return current;
        }

        private TransformProperty GetTransformProperty(string n)
        {
            return n switch
            {
                "m_LocalPosition" => TransformProperty.LocalPosition,
                "m_LocalRotation" => TransformProperty.LocalRotation,
                "m_LocalScale" => TransformProperty.LocalScale,
                "localEulerAnglesRaw" => TransformProperty.LocalEulerAngles,
                _ => TransformProperty.None,
            };
        }

        private VectorProperty GetVectorProperty(string n)
        {
            return n switch
            {
                "x" => VectorProperty.X,
                "y" => VectorProperty.Y,
                "z" => VectorProperty.Z,
                "w" => VectorProperty.W,
                _ => throw new NotImplementedException(),
            };
        }

        private class RuntimeClipCurve : IAnimationCurveEvaluator
        {
            public Transform targetTransform;
            public TransformProperty transformProperty;
            public VectorProperty vectorProperty;
            public AnimationCurve animationCurve;

            public void Evaluate(float time)
            {
                if (transformProperty == TransformProperty.LocalEulerAngles)
                {
                    var oldRotation = targetTransform.localRotation;
                    var v = animationCurve.Evaluate(time);
                    switch (vectorProperty)
                    {
                        case VectorProperty.X: oldRotation.x = 0; oldRotation = Quaternion.AngleAxis(v, Vector3.right) * oldRotation; break;
                        case VectorProperty.Y: oldRotation.y = 0; oldRotation = Quaternion.AngleAxis(v, Vector3.up) * oldRotation; break;
                        case VectorProperty.Z: oldRotation.z = 0; oldRotation = Quaternion.AngleAxis(v, Vector3.forward) * oldRotation; break;
                    }
                    targetTransform.localRotation = oldRotation;
                }
                else if (transformProperty == TransformProperty.LocalRotation)
                {
                    var oldRotation = targetTransform.localRotation;
                    var v = animationCurve.Evaluate(time);
                    switch (vectorProperty)
                    {
                        case VectorProperty.X: oldRotation.x = v; break;
                        case VectorProperty.Y: oldRotation.y = v; break;
                        case VectorProperty.Z: oldRotation.z = v; break;
                        case VectorProperty.W: oldRotation.w = v; break;
                    }
                    targetTransform.localRotation = oldRotation;
                }
                else
                {
                    var v = GetVector();
                    v = SetVectorProperty(v, animationCurve.Evaluate(time));
                    SetVector(v);
                }
            }

            public Vector3 GetVector()
            {
                return transformProperty switch
                {
                    TransformProperty.LocalPosition => targetTransform.localPosition,
                    TransformProperty.LocalScale => targetTransform.localScale,
                    TransformProperty.LocalEulerAngles => targetTransform.localRotation.eulerAngles,
                    TransformProperty.None => throw new NotImplementedException(),
                    TransformProperty.LocalRotation => throw new NotImplementedException(),
                    _ => throw new NotImplementedException(),
                };
            }
            public void SetVector(Vector3 v)
            {
                switch (transformProperty)
                {
                    case TransformProperty.LocalPosition: targetTransform.localPosition = v; break;
                    case TransformProperty.LocalScale: targetTransform.localScale = v; break;
                    case TransformProperty.LocalEulerAngles: targetTransform.localRotation = Quaternion.Euler(v); break;
                }
                ;
            }

            public Vector3 SetVectorProperty(Vector3 v, float value)
            {
                switch (vectorProperty)
                {
                    case VectorProperty.X: v.x = value; break;
                    case VectorProperty.Y: v.y = value; break;
                    case VectorProperty.Z: v.z = value; break;
                }
                return v;
            }
        }

        private enum TransformProperty
        {
            None,
            LocalPosition,
            LocalEulerAngles,
            LocalRotation,
            LocalScale
        }

        private enum VectorProperty
        {
            X, Y, Z, W,
        }

        private interface IAnimationCurveEvaluator
        {
            void Evaluate(float time);
        }
    }
}
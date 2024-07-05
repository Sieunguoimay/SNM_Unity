#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine;
using System.Collections.Generic;

public class AnimationClipAsset : ScriptableObject
{
#if UNITY_EDITOR
    [SerializeField] private AnimationClip clip;
#endif
    [SerializeField] private float clipLength;
    [SerializeField] private ClipCurve[] clipCurves;
    
    public IReadOnlyList<ClipCurve> ClipCurves => clipCurves;
    public float ClipLength => clipLength;

    [Serializable]
    public class ClipCurve
    {
        public string path;
        public string type;
        public string propertyName;
        public AnimationCurve animationCurve;
    }

#if UNITY_EDITOR
    [ContextMenu("LoadAndCache")]
    private void ConvertFromAnimationClip()
    {
        clipLength = clip.length;

        var bindings = AnimationUtility.GetCurveBindings(clip);

        clipCurves = new ClipCurve[bindings.Length];

        // var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);

        for (int i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            var path = binding.path;
            var property = binding.propertyName;

            // if (binding.type == typeof(Transform))
            // {
            clipCurves[i] = new ClipCurve
            {
                path = path,
                type = binding.type.AssemblyQualifiedName,
                propertyName = property,
                animationCurve = curve,
            };
            // }
            // else
            // {
            //     Debug.Log($"Not supported {binding.type.Name}");
            // }
        }
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
#endif
}
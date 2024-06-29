using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Supports.ViewHierachy
{
    public class ReflectionViewNode : ViewNode
    {
#if UNITY_EDITOR
        [StringSelector(nameof(AllTypes), false, true)]
#endif
        [SerializeField] private string reflectionType;

#if UNITY_EDITOR
        protected IEnumerable<string> AllTypes => AssetDatabase.FindAssets("t: MonoScript")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
            .Select(ms => ms.GetClass()?.AssemblyQualifiedName);
#endif

        protected override Type GetReflectionType()
        {
            return Type.GetType(reflectionType);
        }

        protected override object GetReflectionData()
        {
            return DynamicData;
        }
    }
}


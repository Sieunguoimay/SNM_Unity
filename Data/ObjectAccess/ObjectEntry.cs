using System;
using System.Collections.Generic;
using System.Linq;
using InspectorExtensions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace ObjectAccess
{
    public class ObjectEntry : ScriptableObject
    {
        [Tooltip("Empty means no constraint")]
        [StringSelector(nameof(ConstraintTypes))]
        public string type;
        public IEnumerable<string> ConstraintTypes => AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().Select(t => t.AssemblyQualifiedName));
        [field: NonSerialized]
        public ObjectEntryRuntime Runtime { get; set; }
        [RevealNonSerialized]
        private UnityEngine.Object BindedObject => Runtime?.BindedObject as UnityEngine.Object;
    }

}
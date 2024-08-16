using System;
using UnityEngine;

namespace Reflection
{
    public class MonoReflectiveInjector : MonoBehaviour
    {
        [ObjectSelector]
        [SerializeField] private UnityEngine.Object source;
        [SerializeField] private Entry[] entries;

        private void OnEnable()
        {
            foreach (var entry in entries)
            {
                entry.assigner.SetSourceAndDest(source, entry.destination);
                entry.assigner.Assign();
            }
        }

        private void OnDisable()
        {
            foreach (var entry in entries)
            {
                entry.assigner.Unassign();
            }
        }

        [Serializable]
        private class Entry
        {
            [ObjectSelector]
            public UnityEngine.Object destination;
            public ReflectiveFieldAssigner assigner;
        }


#if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(MonoReflectiveInjector), true)]
        private class _Editor : UnityEditor.Editor
        {
            private MonoReflectiveInjector _target;

            void OnEnable()
            {
                _target ??= target as MonoReflectiveInjector;
                SetPreviewSourceObject();
            }

            private void SetPreviewSourceObject()
            {
                foreach (var entry in _target.entries)
                {
                    entry.assigner.SetSourceAndDest(_target.source, entry.destination);
                }
            }
        }
#endif
    }
}


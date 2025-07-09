using System;
using Snm.Tools;
using UnityEngine;

namespace Reflection
{
    [Obsolete]
    public class MonoReflectiveInjector : MonoBehaviour
    {
        [TypeSelector]
        [SerializeField] private UnityEngine.Object source;
        [SerializeField] private bool selfInject = true;
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        private void OnEnable()
        {
            if (selfInject)
            {
                Inject();
            }
        }

        private void OnDisable()
        {
            if (selfInject)
            {
                Eject();
            }
        }

        public void Inject()
        {
            foreach (var entry in entries)
            {
                entry.assigner.SetSourceAndDest(source, entry.destination);
                entry.assigner.Assign();
            }
        }

        public void Eject()
        {
            foreach (var entry in entries)
            {
                entry.assigner.Unassign();
            }
        }

        [Serializable]
        private class Entry
        {
            [TypeSelector]
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
                if (_target == null) return;
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


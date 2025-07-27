using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sieunguoimay.Tools
{
    public class RuntimeObjectExposeComponent : MonoBehaviour, RuntimeObjectExpose.ITargetObjectProvider
    {
        [SerializeField] private Object targetObject;
        public IReadOnlyList<RuntimeObjectExpose.ObjectExposedItem> DisplayItems;

        [ContextMenu("Expose")]
        public void Expose()
        {
            var objectExpose = new RuntimeObjectExpose(this);
            DisplayItems = objectExpose.ExposeObject();
            foreach (var displayItem in DisplayItems)
            {
                Debug.Log($"{displayItem.FieldName} {displayItem.DisplayValue}");
            }
        }

        public object TargetObject => targetObject;
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(RuntimeObjectExposeComponent))]
    public class RuntimeObjectExposeComponentEditor : Editor
    {
        private RuntimeObjectExpose.CommonRuntimeObjectExposeEditor _commonRuntimeObjectExposeEditor;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var component = target as RuntimeObjectExposeComponent;
            if (component == null) return;
            if (component.DisplayItems != null)
            {
                _commonRuntimeObjectExposeEditor ??= new RuntimeObjectExpose.CommonRuntimeObjectExposeEditor(
                    i =>
                    {
                        var window = ObjectBrowserWindow.OpenWindowAndReturnSelf();
                        window.ChangeRootObject(serializedObject.FindProperty("targetObject").objectReferenceValue);
                        window.ResetPath();
                        window.GoInto(i);
                    });
                _commonRuntimeObjectExposeEditor.DrawExposedItems(component.DisplayItems);
            }

            if (GUILayout.Button("Browse"))
            {
                component.Expose();
            }
        }
    }
#endif
}
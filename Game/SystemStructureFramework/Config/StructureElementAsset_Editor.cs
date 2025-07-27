#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Framework.System
{
    [CustomEditor(typeof(StructureElementAsset), true)]
    public class StructureElementAsset_Editor : Editor
    {
        private StructureElementAssetVE _containerVE;

        private void OnEnable()
        {
            UpdateReferenceEntries();

            Debug.Log("OnEnable");
        }

        private void OnDisable()
        {
            Debug.Log($"OnDisable {_containerVE}");

            if (_containerVE != null)
            {
                _containerVE.Dispose();
                _containerVE = null;
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            Debug.Log("CreateInspectorGUI");
            var elementAsset = (StructureElementAsset)target;
            var root = new VisualElement();
            var defaultInspector = CreateEditor(target);
            root.Add(new IMGUIContainer(() =>
            {
                EditorGUI.BeginChangeCheck();
                defaultInspector.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    Debug.Log("IMGUIContainer ChangeCheck");
                    _containerVE.RefreshVE();
                }
            }));
            root.Add(_containerVE = new StructureElementAssetVE(elementAsset));
            return root;
        }

        private void UpdateReferenceEntries()
        {
            var referenceFields = GetReferenceFields().ToArray();
            var elementAsset = (StructureElementAsset)target;
            var existingEntries = elementAsset.Editor_ElementReferences;

            var newEntries = referenceFields
                .Select(f =>
                {
                    var entry = existingEntries.FirstOrDefault(e => e.InjectId == f.Name);
                    if (entry == null)
                    {
                        entry = new StructureElementReferenceEntry(f.Name, null);
                    }
                    entry.SetTargetType(f.FieldType);
                    return entry;
                })
                .ToArray();

            elementAsset.Editor_SetElementReferences(newEntries);

            EditorUtility.SetDirty(elementAsset);
        }

        private IEnumerable<FieldInfo> GetReferenceFields()
        {
            var att = target.GetType().GetCustomAttribute<StructureElementAssetForAttribute>();
            if (att != null)
            {
                return GetReferenceFields(att.ConfigType);
            }
            return Enumerable.Empty<FieldInfo>();
        }

        private static IEnumerable<FieldInfo> GetReferenceFields(Type type)
        {
            var currentType = type;

            while (currentType != null)
            {
                var fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<ElementReferenceAttribute>() != null)
                    {
                        yield return field;
                    }
                }

                currentType = currentType.BaseType;
            }
        }
    }


}
#endif
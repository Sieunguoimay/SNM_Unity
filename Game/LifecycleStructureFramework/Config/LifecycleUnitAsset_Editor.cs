#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Snm.LifecycleStructureFramework
{
    [CustomEditor(typeof(LifecycleUnitAsset), true)]
    public class LifecycleUnitAsset_Editor : Editor
    {
        private void OnEnable()
        {
            var unitAsset = (LifecycleUnitAsset)target;
            var att = target.GetType().GetCustomAttribute<LifecycleUnitAssetForAttribute>();

            if (att != null && att.LifecycleUnitType != null)
            {
                UpdateReferenceEntries(unitAsset, att.LifecycleUnitType);
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var unitAsset = (LifecycleUnitAsset)target;

            var anyChanged = false;

            foreach (var reference in unitAsset.UnitReferences)
            {
                anyChanged |= DrawElementReferenceEntry(reference);
            }

            if (anyChanged)
            {
                EditorUtility.SetDirty(unitAsset);
            }
        }

        private bool DrawElementReferenceEntry(LifecycleUnitReferenceEntry entry)
        {
            var result = false;
            var color = GUI.color;
            GUI.color = entry.Asset != null ? color : Color.red;
            var newAsset = (LifecycleUnitAsset)EditorGUILayout.ObjectField(entry.InjectId, entry.Asset, typeof(LifecycleUnitAsset), false);
            if (newAsset != entry.Asset)
            {
                entry.SetAsset(newAsset);
                result = true;
            }
            GUI.color = color;
            return result;
        }

        private void UpdateReferenceEntries(LifecycleUnitAsset unitAsset, Type unitType)
        {
            var fields = GetReferenceFields(unitType);
            var newEntries = fields
                .Select(f => new LifecycleUnitReferenceEntry(
                    injectId: f,
                    asset: unitAsset.UnitReferences.FirstOrDefault(ef => ef.InjectId == f)?.Asset))
                .ToArray();
            unitAsset.SetUnitReferences(newEntries);
        }

        private static IEnumerable<string> GetReferenceFields(Type type)
        {
            var currentType = type;
            while (currentType != null)
            {
                var fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<InjectUnitAttribute>() != null)
                    {
                        yield return field.Name;
                    }
                }
                currentType = currentType.BaseType;
            }
        }
    }
}
#endif
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Framework.System
{
    public class StructureElementAssetVE : VisualElement, IDisposable
    {
        private readonly StructureElementAsset elementAsset;
        private readonly Foldout foldout_References;
        private readonly VisualElement container_Structures;
        private IReadOnlyList<StructureElementReferenceEntry> _cachedReferenceList;

        public StructureElementAssetVE(StructureElementAsset elementAsset)
        {
            this.elementAsset = elementAsset;

            Add(foldout_References = new Foldout() { text = "Element References" });
            UpdateReferenceVEList();

            Foldout foldout_StructureAssets;
            VisualElement horizontal;
            Add(foldout_StructureAssets = new Foldout() { text = "Found in StructureAssets" });
            foldout_StructureAssets.Add(container_Structures = new VisualElement());
            foldout_StructureAssets.Add(horizontal = new VisualElement() { style = { flexDirection = FlexDirection.Row } });
            horizontal.Add(new Button(AddElementToNearestStructure) { text = "Add to Nearest StructureAsset" });
            horizontal.Add(new Button(RefillAllStructureAssets) { text = "Refill all StructureAssets" });

            UpdateStructureVEList();
        }

        public void Dispose()
        {
            foreach (var eve in Children().OfType<StructureElementReferenceEntryVE>())
            {
                eve.Dispose();
            }

            if (_cachedReferenceList != null)
            {
                foreach (var r in _cachedReferenceList)
                {
                    r.OnDefinitionAssetChanged -= AnyReference_OnDefinitionAssetChanged;
                }
                _cachedReferenceList = null;
            }

            Debug.Log($"{nameof(StructureElementAsset)} Disposed");
        }


        private void UpdateStructureVEList()
        {
            container_Structures.Clear();
            foreach (var structureAsset in GetStructureAssets_ContainingThisElement())
            {
                container_Structures.Add(new ObjectField() { value = structureAsset });
            }
        }

        private void AddElementToNearestStructure()
        {
            var elementPath = AssetDatabase.GetAssetPath(elementAsset);

            var maxPathLength = int.MinValue;
            SystemStructureAsset nearestStructure = null;

            foreach (var structure in SystemStructureAsset.GetAllStructureAssets())
            {
                if (structure.ElementAssets.Contains(elementAsset))
                {
                    structure.SetElementAssets(structure.ElementAssets.Where(e => e != elementAsset).ToArray());
                }

                var pp = AssetDatabase.GetAssetPath(structure.SelectedFolder);

                if (elementPath.StartsWith(pp + "/"))
                {
                    var pathLength = pp.Length;
                    if (pathLength > maxPathLength)
                    {
                        maxPathLength = pathLength;
                        nearestStructure = structure;
                    }
                }
            }

            if (nearestStructure != null)
            {
                nearestStructure.SetElementAssets(nearestStructure.ElementAssets.Append(elementAsset).ToArray());

                EditorUtility.SetDirty(nearestStructure);
                Undo.RecordObject(nearestStructure, "Added Element to StructureAsset");

                Debug.Log($"Added {elementAsset.name} to StructureAsset " + nearestStructure.name, nearestStructure);
            }
            else
            {
                Debug.LogError("No StructureAsset found");
            }

            UpdateStructureVEList();
        }

        private void RefillAllStructureAssets()
        {
            SystemStructureAsset.RefillAllStructureAssets();
        }

        public void RefreshVE()
        {
            UpdateReferenceVEList();
        }

        private void UpdateReferenceVEList()
        {
            foreach (var eve in foldout_References.Children().OfType<StructureElementReferenceEntryVE>())
            {
                eve.Dispose();
            }
            foldout_References.Clear();

            if (_cachedReferenceList != null)
            {
                foreach (var r in _cachedReferenceList)
                {
                    r.OnDefinitionAssetChanged -= AnyReference_OnDefinitionAssetChanged;
                }
            }

            _cachedReferenceList = elementAsset.Editor_ElementReferences;

            if (_cachedReferenceList != null)
            {
                foreach (var r in _cachedReferenceList)
                {
                    r.OnDefinitionAssetChanged += AnyReference_OnDefinitionAssetChanged;
                }

                foreach (var er in _cachedReferenceList)
                {
                    foldout_References.Add(new StructureElementReferenceEntryVE(er, () =>
                    {
                        return GetStructureAssets_ContainingThisElement()
                            .SelectMany(s => s.ElementAssets)
                            .Where(o =>
                            {
                                var att = o.GetType().GetCustomAttribute<StructureElementAssetForAttribute>();
                                return att == null || er.Editor_TargetType.IsAssignableFrom(att.ElementType);
                            })
                            .ToArray();
                    }));
                }
            }
        }

        private void AnyReference_OnDefinitionAssetChanged(StructureElementReferenceEntry entry)
        {
            SaveElementAsset();
        }

        private void SaveElementAsset()
        {
            EditorUtility.SetDirty(elementAsset);
            Debug.Log($"Saved {elementAsset.name}", elementAsset);
        }

        private IEnumerable<SystemStructureAsset> GetStructureAssets_ContainingThisElement()
        {
            return SystemStructureAsset.GetAllStructureAssets()
                .Where(s => s.ElementAssets.Contains(elementAsset));
        }

        // private IEnumerable<SystemStructureAsset> GetStructureAssets()
        // {
        //     return AssetDatabase.FindAssets($"t:{nameof(SystemStructureAsset)}")
        //         .Select(AssetDatabase.GUIDToAssetPath)
        //         .Select(AssetDatabase.LoadAssetAtPath<SystemStructureAsset>);
        // }
    }


}
#endif
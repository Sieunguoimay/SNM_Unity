#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Framework.System
{
    public class StructureElementAssetVE : Foldout, IDisposable
    {
        private readonly StructureElementAsset elementAsset;
        private readonly ObjectField objectField_StructureAsset;
        private IReadOnlyList<StructureElementReferenceEntry> _cachedReferenceList;

        public StructureElementAssetVE(StructureElementAsset elementAsset)
        {
            text = "Element References";
            this.elementAsset = elementAsset;
            UpdateReferenceVEList();
            Add(objectField_StructureAsset = new ObjectField() { label = "Structure Asset", value = GetStructureAsset()});
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

        public void RefreshVE()
        {
            UpdateReferenceVEList();
        }

        private void UpdateReferenceVEList()
        {
            foreach (var eve in Children().OfType<StructureElementReferenceEntryVE>())
            {
                eve.Dispose();
            }
            Clear();

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
                    Add(new StructureElementReferenceEntryVE(er));
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

        private SystemStructureAsset GetStructureAsset()
        {
            return AssetDatabase.FindAssets($"t:{nameof(SystemStructureAsset)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SystemStructureAsset>)
                .FirstOrDefault(s => s.ElementAssets.Contains(elementAsset));
        }
    }


}
#endif
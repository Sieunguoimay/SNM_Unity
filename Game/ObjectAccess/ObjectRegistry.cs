using System;
using System.Collections.Generic;
using System.Linq;
using InspectorExtensions;
using UnityEditor;
using UnityEngine;

namespace ObjectAccess
{
    public class ObjectRegistry : ScriptableObject
    {
        [SerializeField] private ObjectEntry[] entries;

        [NonSerialized]
        private bool _initialized = false;
        [RevealNonSerialized]
        public ICollection<ObjectEntry> Entries
        {
            get
            {
                if (!_initialized)
                {
                    foreach (var e in entries)
                    {
                        InitEntryRuntime(e);
                    }
                    _initialized = true;
                }
                return entries;
            }
        }

        public void Register(ObjectEntry e, object obj)
        {
            if (!Entries.Contains(e))
            {
                Debug.LogError($"ObjectRegistry: Failed to Register {e.name}! Entry does not exist.");
            }
            else
            {
                if (e.Runtime.Type != null && !e.Runtime.Type.IsInstanceOfType(obj))//constraint type null means valid
                {
                    Debug.LogError($"ObjectRegistry: Failed to Register {e.name}! Object {obj} is not InstanceOfType {e.Runtime.Type.Name}");
                }
                else
                {
                    if (e.Runtime.IsRegistered)
                    {
                        Debug.LogError($"ObjectRegistry: Failed to Register! Entry {e.name} already taken by {e.Runtime.BindedObject}");
                    }
                    else
                    {
                        e.Runtime.BindedObject = obj;
                        Debug.Log($"ObjectRegistry: Registered {e.name} {obj}");
                    }
                }
            }
        }

        public void Unregister(ObjectEntry e)
        {
            if (e.Runtime.IsRegistered)
            {
                Debug.Log($"ObjectRegistry: Unregistered {e.name} {e.Runtime.BindedObject}");
                e.Runtime.BindedObject = null;
            }
            else
            {
                Debug.LogError($"ObjectRegistry: Failed to Unegister! Entry {e.name} already unregistered");
            }
        }

        public bool TryGetObject<TObject>(ObjectEntry e, out TObject obj)
        {
            if (e.Runtime.IsRegistered)
            {
                if (e.Runtime.BindedObject is TObject ot)
                {
                    obj = ot;
                    return true;
                }
                Debug.LogError($"Failed to GetObject of type {typeof(TObject).Name}. Current object is {e.Runtime.BindedObject}");
            }
            else
            {
                Debug.LogError($"Failed to GetObject!. Entry {e.name} is not registered.");
            }
            obj = default;
            return false;
        }

        private void InitEntryRuntime(ObjectEntry entry)
        {
            entry.Runtime = new ObjectEntryRuntime(Type.GetType(entry.type));
        }


#if UNITY_EDITOR

        public void AddEntry_Editor(string entryName, string type)
        {
            var entry = CreateEntry();
            entry.name = entryName;
            entry.type = type;
            EditorUtility.SetDirty(entry);
            AssetDatabase.SaveAssetIfDirty(entry);
        }

        [ContextMenu("AddEntry")]
        private ObjectEntry CreateEntry()
        {
            var entry = CreateInstance<ObjectEntry>();
            AssetDatabase.AddObjectToAsset(entry, AssetDatabase.GetAssetPath(this));
            entries = entries.Append(entry).ToArray();
            return entry;
        }
#endif
    }

}
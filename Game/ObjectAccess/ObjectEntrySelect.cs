using System;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ObjectAccess
{
    [Serializable]
    public class RegistryEntrySelector
    {
        [SerializeField] private ObjectRegistry registry;
        [SerializeField] private ObjectEntry entry;

        public ObjectEntry Entry => entry;
        public string EntryName => entry.name;
        public ObjectRegistry Registry => registry;

        public bool TryGetObject<TObject>(out TObject obj) where TObject : UnityEngine.Object => Registry.TryGetObject(Entry, out obj);
    }


#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(RegistryEntrySelector))]
    public class ObjectEntrySelectDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.serializedObject.Update();
            var entry = property.FindPropertyRelative("entry");
            var registry = property.FindPropertyRelative("registry");
            position.width = position.width / 3f * 2f;
            EditorGUI.ObjectField(position, registry, label);
            position.x += position.width;
            position.width /= 2f;
            var color = GUI.color;
            var reg = registry.objectReferenceValue as ObjectRegistry;
            if (reg != null)
            {
                var valid = reg.Entries.Contains(entry.objectReferenceValue);
                GUI.color = valid ? color : Color.red;
            }
            if (EditorGUI.DropdownButton(position, new GUIContent(entry.objectReferenceValue?.name), FocusType.Passive))
            {
                var menu = new GenericMenu();
                if (registry.objectReferenceValue is ObjectRegistry r)
                {
                    foreach (var e in r.Entries)
                    {
                        menu.AddItem(new GUIContent(e.name), false, () =>
                        {
                            entry.objectReferenceValue = reg.Entries.FirstOrDefault(i => i.name == e.name);
                            property.serializedObject.ApplyModifiedProperties();
                        });
                    }
                }
                menu.ShowAsContext();
            }
            GUI.color = color;
        }
    }
#endif

}
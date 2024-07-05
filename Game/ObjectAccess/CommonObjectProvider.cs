using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ObjectAccess
{
    [Serializable]
    public class CommonObjectProvider
    {
        [SerializeField] private RegistryEntrySelector selector;
        [SerializeField] private AccessType accessType;
        [ObjectSelector]
        [SerializeField] private UnityEngine.Object directReference;

        public bool TryGetObject<TObject>(out TObject obj) where TObject : UnityEngine.Object
        {
            switch (accessType)
            {
                case AccessType.Direct:
                    if (directReference is TObject o)
                    {
                        obj = o;
                        return true;
                    }
                    else
                    {
                        obj = null;
                        return false;
                    }
                case AccessType.Registry:
                    return selector.TryGetObject(out obj);
                default:
                    obj = null;
                    return false;
            }
        }
    }

    public enum AccessType
    {
        Direct,
        Registry
    }


#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(CommonObjectProvider))]
    public class ObjectAccessorDrawer : ObjectEntrySelectDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // var useLocal = property.FindPropertyRelative("useLocal");
            var accessType = property.FindPropertyRelative("accessType");
            var directReference = property.FindPropertyRelative("directReference");

            position.height = EditorGUIUtility.singleLineHeight + 1;

            EditorGUI.BeginChangeCheck();
            position.width /= 2;
            EditorGUI.LabelField(position, label);
            position.x += position.width;
            if (EditorGUI.DropdownButton(position, new GUIContent(accessType.enumNames[accessType.enumValueIndex]), FocusType.Passive))
            {
                SelectAccessType(property, accessType);
            }
            position.x -= position.width;
            position.width *= 2;
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }

            position.y += position.height;
            position.x += 8;
            position.width -= 8;
            if (accessType.intValue == (int)AccessType.Direct)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(position, directReference);
                if (EditorGUI.EndChangeCheck())
                {
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                base.OnGUI(position, property, new GUIContent("Global Reference"));
            }
        }

        private static void SelectAccessType(SerializedProperty property, SerializedProperty accessType)
        {
            var menu = new GenericMenu();
            foreach (var e in typeof(AccessType).GetEnumValues())
            {
                menu.AddItem(new GUIContent(e.ToString()), accessType.intValue == (int)e, () =>
                {
                    accessType.intValue = (int)e;
                    property.serializedObject.ApplyModifiedProperties();
                    AssetDatabase.SaveAssetIfDirty(property.serializedObject.targetObject);
                });
            }
            menu.ShowAsContext();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + 2;
        }
    }
#endif
}
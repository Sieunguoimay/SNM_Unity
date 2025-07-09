#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools
{
    public class ReflectionExposerWindow : EditorWindow
    {
        [SerializeField] private string typeName;

        private VisualElement _button_LogAllMembers;
        private VisualElement _textField_TypeName;

        [MenuItem("Tools/ReflectionExposer")]
        private static void Open()
        {
            GetWindow<ReflectionExposerWindow>().Show();
        }

        private void CreateGUI()
        {
            var rootVE = rootVisualElement;

            rootVE.Add(_textField_TypeName = new TextField
            {
                value = typeName,
            });

            _textField_TypeName.RegisterCallback<ChangeEvent<string>>(TextField_TypeName_OnValueChanged);

            rootVE.Add(_button_LogAllMembers = new Button(Button_LogAllMembers_OnClicked)
            {
                text = "LogAllMembers"
            });
        }

        private void TextField_TypeName_OnValueChanged(ChangeEvent<string> evt)
        {
            typeName = evt.newValue;
        }

        private void Button_LogAllMembers_OnClicked()
        {
            LogAllMembers();
        }

        private void LogAllMembers()
        {
            InspectType(FindTypeByName(typeName));
        }

        public static Type FindTypeByName(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        public static void InspectType(Type type)
        {
            if (type == null)
            {
                Debug.LogError("type class not found.");
                return;
            }

            var members = type.GetMembers(BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static);

            foreach (var member in members)
            {
                Debug.Log($"{member}");
            }
        }
    }
}
#endif
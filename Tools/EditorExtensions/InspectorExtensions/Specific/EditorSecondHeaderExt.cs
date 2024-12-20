#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace InspectorExtensions
{
    public class EditorSecondHeaderExt : IInspectorExtension
    {
        ExtensionType IInspectorExtension.ExtensionType => ExtensionType.Object;
        ExtensionPosition IInspectorExtension.Position => ExtensionPosition.Top;
        int IInspectorExtension.Priority => 0;

        bool IInspectorExtension.IsSupportedFor(object target)
        {
            if (target is MonoBehaviour) return true;
            if (target is ScriptableObject) return true;
            return false;
        }

        void IInspectorExtension.ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            if (extensionElement.Target is Object target)
            {
                extensionElement.Add(new SecondHeaderVE(target));
            }
        }

        void IInspectorExtension.CleanUp()
        {
        }

        private class SecondHeaderVE : VisualElement
        {
            private readonly Object target;

            public SecondHeaderVE(Object target)
            {
                this.target = target;

                style.flexDirection = FlexDirection.RowReverse;

                if (target is ScriptableObject)
                {
                    style.paddingTop = 2;
                    style.paddingBottom = 2;
                    style.height = 22;
                }
                if (target is MonoBehaviour)
                {
                    style.marginBottom = -3;
                    style.height = 18;
                }

                Add(CreateEditScriptButton());
                Add(CreateFindReferencesInSceneButton());
            }

            private VisualElement CreateEditScriptButton()
            {
                var button = new Button(OnEditScriptButtonClicked)
                {
                    text = "Edit Script"
                };
                button.style.marginTop = 0;
                button.style.marginBottom = 0;
                return button;
            }

            private void OnEditScriptButtonClicked()
            {
                if (target != null)
                {
                    var serialized = new SerializedObject(target);
                    var scriptProperty = serialized.FindProperty("m_Script");
                    AssetDatabase.OpenAsset(scriptProperty.objectReferenceValue);
                }
            }


            private VisualElement CreateFindReferencesInSceneButton()
            {
                var button = new Button(OnFindReferencesInSceneClicked)
                {
                    text = "Find References in Scene"
                };
                button.style.marginTop = 0;
                button.style.marginBottom = 0;
                return button;
            }

            private void OnFindReferencesInSceneClicked()
            {
                typeof(SearchableEditorWindow)
                    .GetMethod("SearchForReferencesToInstanceID", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { target.GetInstanceID() });
            }

            private static IEnumerable<(System.Type type, MethodInfo methodInfo, MenuItem)> FindAllMenuItems()
            {
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            MenuItem mi = null;
                            try { mi = method.GetCustomAttribute<MenuItem>(); } catch (System.Exception) { }
                            if (mi != null)
                            {
                                yield return (type, method, mi);
                            }

                        }
                    }
                }
            }
        }
    }
}

#endif
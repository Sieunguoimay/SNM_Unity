#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace InspectorExtensions
{
    public class EditorSecondHeaderVE : VisualElement
    {
        private readonly Object target;

        private readonly VisualElement buttonsContainer;
        public VisualElement ButtonsContainer => buttonsContainer;

        public EditorSecondHeaderVE(Object target)
        {
            this.target = target;

            var foldout = new ReferencesFoldout(target);
            var toggle = foldout.Q<Toggle>();
            buttonsContainer = new VisualElement();
            buttonsContainer.style.flexDirection = FlexDirection.RowReverse;
            buttonsContainer.style.flexGrow = 1;
            toggle.style.marginBottom = 0;
            toggle.style.marginTop = 0;
            toggle.style.marginRight = 0;
            toggle.Add(buttonsContainer);
            Add(foldout);

            style.backgroundColor = new Color(0f, 0f, 0f, .2f);
            style.marginBottom = 4;

            if (target is MonoBehaviour || target is ScriptableObject) buttonsContainer.Add(CreateEditScriptButton());

            buttonsContainer.Add(CreateFindReferencesInSceneButton());

            if (target is not Component)
            {
                if (target is not GameObject go || !go.scene.isLoaded)
                {
                    buttonsContainer.Add(CreateFindReferencesInProjectButton());
                }

                if (target is ScriptableObject)
                {
                    CreateMenuItemObjects();
                    buttonsContainer.Add(_copyComponentMenuItem);
                    buttonsContainer.Add(_pasteComponentValuesMenuItem);
                }
            }
            else
            {
                if (target is Transform)
                {
                    return;
                }

                CreateMenuItemObjects();
                buttonsContainer.Add(_copyComponentMenuItem);
                buttonsContainer.Add(_pasteComponentValuesMenuItem);
            }

            InspectorExtensionInstaller.Instance.InspectorWindow.rootVisualElement.RegisterCallback<MouseEnterEvent>(OnRepaint);

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            InspectorExtensionInstaller.Instance.InspectorWindow.rootVisualElement.UnregisterCallback<MouseEnterEvent>(OnRepaint);
        }

        private void OnRepaint(MouseEnterEvent evt)
        {
            Refresh();
        }

        public void Refresh()
        {
            _copyComponentMenuItem?.Refresh();
            _pasteComponentValuesMenuItem?.Refresh();
            _pasteComponentAsNewMenuItem?.Refresh();
        }

        private VisualElement CreateEditScriptButton()
        {
            var button = new Button(OnEditScriptButtonClicked)
            {
                text = "Edit Script"
            };
            button.style.marginTop = 0;
            button.style.marginBottom = 0;
            button.style.paddingLeft = 6;
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
            button.style.paddingLeft = 6;
            return button;
        }

        private void OnFindReferencesInSceneClicked()
        {
            typeof(SearchableEditorWindow)
                .GetMethod("SearchForReferencesToInstanceID", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { target.GetInstanceID() });
        }

        private VisualElement CreateFindReferencesInProjectButton()
        {
            var button = new Button(OnFindReferencesInProjectClicked)
            {
                text = "Find References in Project"
            };
            button.style.marginTop = 0;
            button.style.marginBottom = 0;
            button.style.paddingLeft = 6;
            return button;
        }

        private void OnFindReferencesInProjectClicked()
        {
            typeof(SearchableEditorWindow)
                .GetMethod("SearchForReferencesInProject", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { target });
        }

        private void LogAllMenuItems()
        {
            foreach (var mi in FindAllMenuItems())
            {
                Debug.Log(mi.Item1 + "|" + mi.Item2 + "|" + mi.Item3.menuItem);
            }
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

        private MenuItemButton _copyComponentMenuItem;
        private MenuItemButton _pasteComponentValuesMenuItem;
        private MenuItemButton _pasteComponentAsNewMenuItem;

        private void CreateMenuItemObjects()
        {
            if (target is ScriptableObject scriptableObject)
            {
                _copyComponentMenuItem = new MenuItemButton(new FakeMenuItemObject_ScriptableObject_Copy(scriptableObject), "Copy");
                _pasteComponentValuesMenuItem = new MenuItemButton(new FakeMenuItemObject_ScriptableObject_Paste(scriptableObject), "Paste");
            }
            else
            {

                _copyComponentMenuItem = new MenuItemButton(new MenuItemObject("CONTEXT/Component/Copy Component", target), "Copy");
                _pasteComponentValuesMenuItem = new MenuItemButton(new MenuItemObject("CONTEXT/Component/Paste Component Values", target), "Paste");
                _pasteComponentAsNewMenuItem = new MenuItemButton(new MenuItemObject("CONTEXT/Component/Paste Component As New", target), "Paste Component As New");
            }
        }

        private class MenuItemButton : Button
        {
            private readonly IMenuItemObject menuItemObject;

            public MenuItemButton(IMenuItemObject menuItemObject, string displayText)
            {
                this.menuItemObject = menuItemObject;

                text = displayText;
                style.marginTop = 0;
                style.marginBottom = 0;
                style.paddingLeft = 6;
                clicked += OnButtonClicked;

                Refresh();
            }

            private void OnButtonClicked()
            {
                menuItemObject.Execute();
            }

            public void Refresh()
            {
                var value = menuItemObject.IsEnabled();
                style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private class ReferencesFoldout : Foldout
        {
            public ReferencesFoldout(UnityEngine.Object obj)
            {
                var serializedObject = new SerializedObject(obj);
                serializedObject.Update();
                var references = Iterate(serializedObject).Select(o => new { o.propertyPath, o.objectReferenceValue }).ToArray();
                var count = references.Length;

                text = $"References ({count})";
                value = false;
                style.color = Color.gray;
                style.borderTopWidth = 1;
                style.borderTopColor = new Color(.1f, .1f, .1f, 1f);

                if (InspectorExtensionInstaller.Instance.DebugEnabled)
                {
                    Debug.Log($"RevealReferenceEditorExt for {obj.name} ({obj.GetType().Name})");
                }


                foreach (var rObject in references)
                {
                    var foldout = new ObjectField()
                    {
                        label = $"{rObject.propertyPath}: ",
                        value = rObject.objectReferenceValue,
                        // tooltip = $"{r.propertyPath}"
                    };

                    Add(foldout);
                }
            }
        }

        private static IEnumerable<SerializedProperty> Iterate(SerializedObject obj)
        {
            var it = obj.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null)
                {
                    yield return it;
                }
            }
        }

        public interface IMenuItemObject
        {
            bool IsEnabled();
            void Execute();
        }

        public class FakeMenuItemObject_ScriptableObject_Paste : IMenuItemObject
        {
            private readonly ScriptableObject target;

            public FakeMenuItemObject_ScriptableObject_Paste(ScriptableObject target)
            {
                this.target = target;
            }

            void IMenuItemObject.Execute()
            {
                EditorUtility.CopySerialized(ScriptableObjectInspectorExt.CopiedObject, target);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            bool IMenuItemObject.IsEnabled()
            {
                return ScriptableObjectInspectorExt.CopiedObject != null
                && target.GetType().IsAssignableFrom(ScriptableObjectInspectorExt.CopiedObject.GetType());
            }
        }

        public class FakeMenuItemObject_ScriptableObject_Copy : IMenuItemObject
        {
            private readonly ScriptableObject target;

            public FakeMenuItemObject_ScriptableObject_Copy(ScriptableObject target)
            {
                this.target = target;
            }

            void IMenuItemObject.Execute()
            {
                ScriptableObjectInspectorExt.CopiedObject = target;
                InspectorExtensionInstaller.Instance.Refresh();
            }

            bool IMenuItemObject.IsEnabled()
            {
                return true;
            }
        }

        public class MenuItemObject : IMenuItemObject
        {
            private readonly string menuItemPath;
            private readonly Object context;

            public MenuItemObject(string menuItemPath, UnityEngine.Object context)
            {
                this.menuItemPath = menuItemPath;
                this.context = context;
            }

            bool IMenuItemObject.IsEnabled()
            {
                return EditorApplicationHelper.GetEnabledWithContext(menuItemPath, context);
            }

            void IMenuItemObject.Execute()
            {
                EditorApplicationHelper.ExecuteMenuItem(menuItemPath, context);
                InspectorExtensionInstaller.Instance.Refresh();
            }
        }
    }
}

#endif
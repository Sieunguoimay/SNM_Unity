#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class CopyAndPasteMultipleComponentsWindow : EditorWindow, IHasCustomMenu
{
    private ClipboardVE clipboardVE;
    [SerializeField] private GameObject srcGameObject;
    [SerializeField] private GameObject dstGameObject;
    [SerializeField] private List<Component> ignoredSrcComponents = new();
    [SerializeField] private List<Component> ignoredDstComponents = new();
    [SerializeField] private bool selfGameObject = false;

    [MenuItem("GameObject/Copy And Paste All Components Values")]
    [MenuItem("CONTEXT/Component/Copy And Paste All Components Values")]
    public static void CopyGameObject2(MenuCommand menuCommand)
    {
        var go = (menuCommand.context is Component c)
            ? c.gameObject
            : (menuCommand.context as GameObject);
        if (go != null)
        {
            var w = EditorWindow.GetWindow(typeof(CopyAndPasteMultipleComponentsWindow)) as CopyAndPasteMultipleComponentsWindow;
            w.srcGameObject = go;
            w.clipboardVE.Refresh();
            w.Show();
        }
    }

    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(CopyAndPasteMultipleComponentsWindow)).Show();
    }

    public void AddItemsToMenu(GenericMenu menu)
    {
        menu.AddItem(new GUIContent("Xooo"), false, () => { });
    }

    private void CreateGUI()
    {
        rootVisualElement.Add(clipboardVE = new ClipboardVE(this));
    }

    private void OnGUI()
    {
        if (Event.current != null && Event.current.type! == EventType.Repaint && Event.current.type! == EventType.Layout)
        {
            Debug.Log(Event.current.type);
        }
    }

    private class ClipboardVE : VisualElement
    {
        private readonly CopyAndPasteMultipleComponentsWindow window;
        private readonly ObjectField srcGameObject;
        private readonly ObjectField dstGameObject;
        private readonly VisualElement leftComponents;
        private readonly VisualElement rightComponents;
        private readonly Button button;
        private readonly Label status;
        private readonly Toggle selfGameObject;

        public ClipboardVE(CopyAndPasteMultipleComponentsWindow window)
        {
            this.window = window;
            var scroll = new ScrollView();
            var content = new VisualElement();
            var left = new VisualElement();
            var right = new VisualElement();
            content.style.flexDirection = FlexDirection.Row;
            content.style.display = DisplayStyle.Flex;
            left.style.width = new StyleLength(Length.Percent(50));
            right.style.width = new StyleLength(Length.Percent(50));
            content.Add(left);
            VisualElement space;
            content.Add(space = new Label("->"));
            space.style.marginTop = 22;
            space.style.unityTextAlign = TextAnchor.UpperCenter;
            content.Add(right);
            scroll.Add(content);
            Add(scroll);
            Add(status = new Label() { text = "Only copy when both sides are the same!" });
            Add(button = new Button() { text = "Copy & Paste All Components Values" });
            Add(selfGameObject = new Toggle() { text = "Self GameObject", value = window.selfGameObject });
            selfGameObject.style.alignSelf = Align.FlexEnd;
            selfGameObject.RegisterCallback<ChangeEvent<bool>>(_ =>
            {
                window.selfGameObject = _.newValue;
                UpdateComponentLists();
                UpdateStatus();
            });
            status.style.unityTextAlign = TextAnchor.MiddleCenter;
            status.style.height = 50;
            status.style.fontSize = 20;
            status.style.unityFontStyleAndWeight = FontStyle.Italic;
            button.RegisterCallback<ClickEvent>(evt =>
            {
                CopyAndPaste();
            });
            Label label;
            left.Add(label = new Label("Source"));
            label.style.fontSize = 17;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.unityFontStyleAndWeight = FontStyle.Italic;
            left.Add(srcGameObject = new ObjectField
            {
                value = window.srcGameObject,
                objectType = typeof(GameObject),
                allowSceneObjects = true
            });
            left.Add(leftComponents = new VisualElement());
            leftComponents.style.marginTop = 3;
            leftComponents.style.backgroundColor = ColorUtility.TryParseHtmlString("#292929", out var c) ? c : Color.clear;
            right.Add(label = new Label("Destination"));
            label.style.fontSize = 17;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.unityFontStyleAndWeight = FontStyle.Italic;
            right.Add(dstGameObject = new ObjectField
            {
                value = window.dstGameObject,
                objectType = typeof(GameObject),
                allowSceneObjects = true
            });
            right.Add(rightComponents = new VisualElement());
            rightComponents.style.marginTop = 3;
            rightComponents.style.backgroundColor = ColorUtility.TryParseHtmlString("#292929", out c) ? c : Color.clear;
            srcGameObject.RegisterValueChangedCallback(OnSrcGameObjectChanged);
            dstGameObject.RegisterValueChangedCallback(OnDstGameObjectChanged);
            UpdateComponentLists();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            var valid = Validate();
            button.SetEnabled(valid);
            status.text = valid ? "You're good to go." : "Only copy when both sides are the same!";
        }

        private void CopyAndPaste()
        {
            var src = (selfGameObject.value
                ? window.srcGameObject.GetComponents<Component>()
                : window.srcGameObject.GetComponentsInChildren<Component>())
                .Where(c => !window.ignoredSrcComponents.Contains(c))
                .ToArray();
            var dst = (selfGameObject.value
                ? window.dstGameObject.GetComponents<Component>()
                : window.dstGameObject.GetComponentsInChildren<Component>())
                .Where(c => !window.ignoredDstComponents.Contains(c))
                .ToArray();
            var srcObjs = Array.Empty<UnityEngine.Object>()
                .Concat(src).Concat(src.Select(c => c.gameObject)).Distinct().ToArray();
            var dstObjs = Array.Empty<UnityEngine.Object>()
                .Concat(dst).Concat(dst.Select(c => c.gameObject)).Distinct().ToArray();

            if (src.Length != dst.Length) return;

            for (var i = 0; i < src.Length; i++)
            {
                CopyComponent(src[i], dst[i], srcObjs, dstObjs);
            }

            Debug.Log("All Copied!");
        }

        private void OnDstGameObjectChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            window.dstGameObject = evt.newValue as GameObject;
            UpdateComponentLists();
            UpdateStatus();
        }

        private void OnSrcGameObjectChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            window.srcGameObject = evt.newValue as GameObject;
            UpdateComponentLists();
            UpdateStatus();
        }

        public void Refresh()
        {
            srcGameObject.value = window.srcGameObject;
            dstGameObject.value = window.dstGameObject;
        }

        public void UpdateComponentLists()
        {
            leftComponents.Clear();
            rightComponents.Clear();
            if (window.srcGameObject != null)
            {
                CreateLines(leftComponents, window.srcGameObject.transform, window.ignoredSrcComponents, 0);
            }
            if (window.dstGameObject != null)
            {
                CreateLines(rightComponents, window.dstGameObject.transform, window.ignoredDstComponents, 0);
            }
        }

        private bool Validate()
        {
            if (window.srcGameObject == null || window.dstGameObject == null) return false;
            var src = (selfGameObject.value
                ? window.srcGameObject.GetComponents<Component>()
                : window.srcGameObject.GetComponentsInChildren<Component>())
                .Where(c => !window.ignoredSrcComponents.Contains(c))
                .ToArray();
            var dst = (selfGameObject.value
                ? window.dstGameObject.GetComponents<Component>()
                : window.dstGameObject.GetComponentsInChildren<Component>())
                .Where(c => !window.ignoredDstComponents.Contains(c))
                .ToArray();

            if (src.Length != dst.Length) return false;
            for (var i = 0; i < src.Length; i++)
            {
                if (src[i].GetType() != dst[i].GetType())
                {
                    return false;
                }
            }
            return true;
        }

        private void CreateLines(VisualElement container, Transform tt, List<Component> ignoredComponents, int depth)
        {
            Foldout line;
            container.Add(line = new Foldout());
            line.text = tt.name;

            foreach (var c in tt.GetComponents<Component>())
            {
                VisualElement line2;
                ObjectField f;
                Toggle ignore;
                line.Add(line2 = new VisualElement());
                line2.style.flexDirection = FlexDirection.Row;
                line2.Add(ignore = new Toggle() { value = !ignoredComponents.Contains(c) });
                line2.Add(f = new ObjectField
                {
                    value = c,
                });
                f.SetEnabled(false);
                f.style.backgroundColor = ignoredComponents.Contains(c) ? Color.red : Color.clear;
                ignore.RegisterCallback<ChangeEvent<bool>>(evt =>
                {
                    if (evt.newValue)
                    {
                        ignoredComponents.Remove(c);
                    }
                    else
                    {
                        if (!ignoredComponents.Contains(c))
                        {
                            ignoredComponents.Add(c);
                        }
                    }
                    f.style.backgroundColor = ignoredComponents.Contains(c) ? Color.red : Color.clear;
                    UpdateStatus();
                });
            }

            if (selfGameObject.value) return;

            for (var i = 0; i < tt.childCount; i++)
            {
                CreateLines(line, tt.GetChild(i), ignoredComponents, depth + 1);
            }
        }
    }

    public static void CopyComponent(Component source, Component destination, UnityEngine.Object[] src, UnityEngine.Object[] dst)
    {
        if (source.GetType() != destination.GetType())
        {
            Debug.LogError("Source and destination components are not of the same type!");
            return;
        }

        var sourceSerializedObject = new SerializedObject(source);
        var destSerializedObject = new SerializedObject(destination);

        var srcProperty = sourceSerializedObject.GetIterator();

        srcProperty.Next(true);

        while (srcProperty.NextVisible(true))
        {
            if (srcProperty.propertyType == SerializedPropertyType.ObjectReference)
            {
                var index = Array.IndexOf(src, srcProperty.objectReferenceValue);
                if (index >= 0)
                {
                    srcProperty.objectReferenceValue = dst[index];//Temporary modifying
                }
            }
            destSerializedObject.CopyFromSerializedProperty(srcProperty);
        }

        destSerializedObject.ApplyModifiedProperties();
    }
}

#endif

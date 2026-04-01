#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools
{
    public class VisualElementToolViewerWindow : EditorWindow
    {
        [SerializeField] private UnityEngine.Object _targetObj;

        private readonly Dictionary<string, bool> foldoutStates = new();

        private ObjectField _objectField_Target;
        private VisualElement _horizontal;
        private ScrollView _scrollView;
        private VisualElement _toolVE;
        private object _targetObj2;

        public static void OpenWindow(object target)
        {
            var window = GetWindow<VisualElementToolViewerWindow>();
            window.SetTarget(target);
            window.Show();
        }

        private void CreateGUI()
        {
            AttachVE();
        }

        private void OnDisable()
        {
            DetachVE();
            foldoutStates.Clear();
        }

        public void SetTarget(object target)
        {
            DetachVE();

            if (target is UnityEngine.Object obj)
            {
                titleContent = new GUIContent(obj.name);
                _targetObj = obj;
                AttachVE();
            }
            else
            {
                _targetObj2 = target;
                AttachVE();
            }
        }

        private bool TryGetTarget(out object target)
        {
            if (_targetObj != null)
            {
                target = _targetObj;
                return true;
            }
            else if (_targetObj2 != null)
            {
                target = _targetObj2;
                return true;
            }

            target = null;
            return false;
        }

        private void AttachVE()
        {
            rootVisualElement.Add(_horizontal = new VisualElement() { style = { flexDirection = FlexDirection.Row } });
            rootVisualElement.Add(_scrollView = new ScrollView() { style = { flexGrow = 1 } });
            _horizontal.Add(_objectField_Target = new ObjectField() { value = _targetObj, style = { flexGrow = 1 } });
            _horizontal.Add(new Button(RefreshVE) { text = "Refresh" });
            _objectField_Target.RegisterValueChangedCallback(evt => SetTarget(evt.newValue));

            if (TryGetTarget(out var target))
            {
                _scrollView.Add(_toolVE = CreateToolVEFor(target));
            }

            LoadFoldoutStates();
        }

        private void RefreshVE()
        {
            DetachVE();
            AttachVE();
        }

        private void DetachVE()
        {
            StoreFoldoutStates();

            rootVisualElement.Clear();

            (_toolVE as IDisposable)?.Dispose();
            _toolVE = null;
        }

        private static VisualElement CreateToolVEFor(object target)
        {
            if (target != null)
            {
                var toolVEType = VisualElementToolForAttribute.TryGetToolVETypeFor(target.GetType());

                if (toolVEType != null)
                {
                    return (VisualElement)Activator.CreateInstance(toolVEType, new object[] { target });
                }
            }

            return new Label($"No VisualElementTool for {target.GetType().Name}. Please create one!") { style = { fontSize = 30, overflow = Overflow.Visible, whiteSpace = WhiteSpace.Normal, unityFontStyleAndWeight = FontStyle.Italic, unityTextAlign = TextAnchor.MiddleCenter } };
        }

        private void LoadFoldoutStates()
        {
            foreach (var foldout in rootVisualElement.Query<Foldout>().ToList())
            {
                var id = GetFoldoutId(foldout);
                if (foldoutStates.TryGetValue(id, out var value))
                {
                    foldout.value = value;
                }
            }
        }

        private void StoreFoldoutStates()
        {
            foldoutStates.Clear();
            foreach (var foldout in rootVisualElement.Query<Foldout>().ToList())
            {
                var id = GetFoldoutId(foldout);
                foldoutStates.Add(id, foldout.value);
            }
        }

        private static string GetFoldoutId(Foldout foldout)
        {
            var pathParts = new List<string>();
            var current = foldout as VisualElement;

            while (current != null)
            {
                string part = !string.IsNullOrEmpty(current.name)
                    ? current.name
                    : current.GetType().Name;

                int indexInParent = current.parent?.IndexOf(current) ?? -1;
                pathParts.Add($"{part}[{indexInParent}]");

                current = current.parent;
            }

            pathParts.Reverse();
            return string.Join("/", pathParts);
        }
    }
}
#endif

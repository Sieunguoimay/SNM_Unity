#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace InspectorExtensions
{
    public class InspectorExtensionHeader : VisualElement
    {
        private readonly List<InspectorExtensionElement> extensionElements = new();
        private readonly ToggleButton toggleButton;

        public InspectorExtensionHeader()
        {
            style.flexDirection = FlexDirection.RowReverse;
            style.borderBottomWidth = 1;
            style.borderBottomColor = new Color(.1f, .1f, .1f, 1f);

            toggleButton = new ToggleButton("ON", "OFF", true, ApplyToggleButton) { tooltip = GetTooltipText() };
            Add(toggleButton);

            var refreshButton = new VisualElement() { tooltip = "Refresh" };
            refreshButton.style.width = 15;
            refreshButton.style.height = 15;
            refreshButton.style.marginRight = 5;
            refreshButton.style.backgroundImage = EditorGUIUtility.IconContent("icon dropdown").image as Texture2D;
            refreshButton.RegisterCallback<ClickEvent>(OnRefreshButtonClicked);
            Add(refreshButton);
            RegisterCallback<ClickEvent>(OnHeaderClicked);
        }

        private void OnHeaderClicked(ClickEvent evt)
        {
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        private string GetTooltipText()
        {
            return "Inspector Extensions for: \n" + string.Join("\n", InspectorExtensionInstaller.Instance.InspectorExtensions.Select(e => $"{e.TargetType.Name}"));
        }

        private void OnRefreshButtonClicked(ClickEvent evt)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Refresh"), false, () =>
            {
                InspectorExtensionInstaller.Instance.TryModify();
            });
            menu.ShowAsContext();
        }

        public void ApplyToggleButton()
        {
            ToggleExtensions(toggleButton.Status);
        }

        private void ToggleExtensions(bool status)
        {
            foreach (var e in extensionElements)
            {
                e.style.display = status ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void AddExtension(InspectorExtensionElement extUIE)
        {
            extensionElements.Add(extUIE);
        }

        public void ClearExtensions()
        {
            foreach (var e in extensionElements)
            {
                e.RemoveFromHierarchy();
            }
            extensionElements.Clear();
        }

        private class ToggleButton : Label
        {
            private readonly string textOn;
            private readonly string textOff;
            private bool status = false;
            public bool Status => status;
            private readonly Action changed;

            public ToggleButton(string textOn, string textOff, bool initialStatus, Action changed)
            {
                this.textOn = textOn;
                this.textOff = textOff;
                this.changed = changed;
                status = initialStatus;
                text = status ? this.textOn : this.textOff;
                style.backgroundColor = new StyleColor() { value = status ? Color.green : Color.black };
                RegisterCallback<ClickEvent>(OnClick);
            }

            private void OnClick(ClickEvent evt)
            {
                status = !status;
                text = status ? textOn : textOff;
                style.backgroundColor = new StyleColor() { value = status ? Color.green : Color.black };
                changed?.Invoke();
            }
        }
    }
}

#endif
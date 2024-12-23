#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace InspectorExtensions
{
    public class ToggleButton : Label
    {
        private readonly string saveKey;
        private readonly string textOn;
        private readonly string textOff;
        private readonly Color colorOn = Color.green;
        private readonly Color colorOff = Color.green;
        private readonly Func<bool> status;
        private bool StatusInternal
        {
            get => EditorPrefs.GetBool(saveKey, true);
            set => EditorPrefs.SetBool(saveKey, value);
        }
        public bool Status => StatusInternal;
        private readonly Action onClicked;

        public ToggleButton(string textOn, string textOff, Color colorOn, Color colorOff, Func<bool> status, Action onClicked, string saveKey)
        {
            this.textOn = textOn;
            this.textOff = textOff;
            this.colorOn = colorOn;
            this.colorOff = colorOff;
            this.onClicked = onClicked;
            this.saveKey = saveKey;
            this.status = status;
            SetInternalStatus(status?.Invoke() ?? StatusInternal);
            // text = StatusInternal ? this.textOn : this.textOff;
            // style.backgroundColor = new StyleColor() { value = StatusInternal ? colorOn : colorOff };
            style.unityTextAlign = TextAnchor.MiddleCenter;
            RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);

            RegisterCallback<MouseEnterEvent>(evt =>
            {
                // var color = StatusInternal ? colorOn : colorOff;
                // style.backgroundColor = color * .75f;
                SetInternalStatus(status?.Invoke() ?? StatusInternal);
                style.backgroundColor = colorOn * .8f;
            });

            RegisterCallback<MouseLeaveEvent>(evt =>
            {
                var color = StatusInternal ? colorOn : colorOff;
                style.backgroundColor = color;
            });

            InspectorExtensionInstaller.Instance.InspectorWindow.rootVisualElement.RegisterCallback<MouseEnterEvent>(OnRepaint);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            InspectorExtensionInstaller.Instance.InspectorWindow.rootVisualElement.UnregisterCallback<MouseEnterEvent>(OnRepaint);
        }

        private void OnRepaint(MouseEnterEvent evt)
        {
            SetInternalStatus(status?.Invoke() ?? StatusInternal);
        }

        private void OnClick(ClickEvent evt)
        {
            SetInternalStatus(!StatusInternal);
            onClicked?.Invoke();
        }

        public void SetInternalStatus(bool status)
        {
            StatusInternal = status;
            text = StatusInternal ? textOn : textOff;
            style.backgroundColor = new StyleColor() { value = StatusInternal ? colorOn : colorOff };
        }
    }
    public class ToggleButton2 : Button
    {
        private readonly string saveKey;
        private readonly string textOn;
        private readonly string textOff;
        private readonly Color colorOn = Color.green;
        private readonly Color colorOff = Color.green;
        private readonly Func<bool> status;
        private bool StatusInternal
        {
            get => EditorPrefs.GetBool(saveKey, true);
            set => EditorPrefs.SetBool(saveKey, value);
        }
        public bool Status => StatusInternal;
        private readonly Action onClicked;

        public ToggleButton2(string textOn, string textOff, Color colorOn, Func<bool> status, Action onClicked, string saveKey)
        {
            this.textOn = textOn;
            this.textOff = textOff;
            this.colorOn = colorOn;
            this.colorOff = style.backgroundColor.value;
            this.onClicked = onClicked;
            this.saveKey = saveKey;
            this.status = status;
            SetInternalStatus(status?.Invoke() ?? StatusInternal);
            style.unityTextAlign = TextAnchor.MiddleCenter;
            RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);

            InspectorExtensionInstaller.Instance.InspectorWindow.rootVisualElement.RegisterCallback<MouseEnterEvent>(OnRepaint);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            InspectorExtensionInstaller.Instance.InspectorWindow.rootVisualElement.UnregisterCallback<MouseEnterEvent>(OnRepaint);
        }

        private void OnRepaint(MouseEnterEvent evt)
        {
            SetInternalStatus(status?.Invoke() ?? StatusInternal);
        }

        private void OnClick(ClickEvent evt)
        {
            SetInternalStatus(!StatusInternal);
            onClicked?.Invoke();
        }

        public void SetInternalStatus(bool status)
        {
            StatusInternal = status;
            text = StatusInternal ? textOn : textOff;
            // style.backgroundColor = new StyleColor() { value = StatusInternal ? colorOn : colorOff };
        }
    }
}

#endif
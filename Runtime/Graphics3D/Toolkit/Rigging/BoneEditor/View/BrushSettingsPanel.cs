#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// VisualElement panel with sliders for brush radius, strength, and falloff,
    /// plus a toggle group for the paint operation (Add / Subtract / Smooth).
    /// Binds directly to a BrushSettings instance.
    /// Only visible when the tool is in Paint mode.
    /// </summary>
    public class BrushSettingsPanel : VisualElement
    {
        private BrushSettings _brush;
        private Slider _radiusSlider;
        private Slider _strengthSlider;
        private Slider _falloffSlider;
        private VisualElement _opGroup;
        private Button _addBtn;
        private Button _subBtn;
        private Button _smoothBtn;

        public BrushSettingsPanel()
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.paddingTop = 4;
            style.paddingBottom = 4;
            style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);

            var brushLabel = new Label("Brush:");
            brushLabel.style.marginRight = 8;
            brushLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            Add(brushLabel);

            // Radius slider
            _radiusSlider = CreateSlider("Radius", 0.01f, 2f, 0.1f);
            _radiusSlider.RegisterValueChangedCallback(evt =>
            {
                if (_brush != null) _brush.radius = evt.newValue;
            });
            Add(_radiusSlider);

            // Strength slider
            _strengthSlider = CreateSlider("Strength", 0.01f, 1f, 0.5f);
            _strengthSlider.RegisterValueChangedCallback(evt =>
            {
                if (_brush != null) _brush.strength = evt.newValue;
            });
            Add(_strengthSlider);

            // Falloff slider
            _falloffSlider = CreateSlider("Falloff", 0.01f, 1f, 0.5f);
            _falloffSlider.RegisterValueChangedCallback(evt =>
            {
                if (_brush != null) _brush.falloff = evt.newValue;
            });
            Add(_falloffSlider);

            // Operation toggle group
            _opGroup = new VisualElement();
            _opGroup.style.flexDirection = FlexDirection.Row;
            _opGroup.style.marginLeft = 12;

            _addBtn = CreateOpButton("Add", BrushSettings.BrushOp.Add);
            _subBtn = CreateOpButton("Sub", BrushSettings.BrushOp.Subtract);
            _smoothBtn = CreateOpButton("Smooth", BrushSettings.BrushOp.Smooth);

            _opGroup.Add(_addBtn);
            _opGroup.Add(_subBtn);
            _opGroup.Add(_smoothBtn);
            Add(_opGroup);
        }

        /// <summary>
        /// Binds this panel to a BrushSettings instance and syncs UI state.
        /// </summary>
        public void Bind(BrushSettings brush)
        {
            _brush = brush;
            if (brush == null) return;

            _radiusSlider.SetValueWithoutNotify(brush.radius);
            _strengthSlider.SetValueWithoutNotify(brush.strength);
            _falloffSlider.SetValueWithoutNotify(brush.falloff);
            UpdateOpButtons();
        }

        /// <summary>
        /// Refreshes the UI to match the current BrushSettings values.
        /// Call this if the settings change externally (e.g., via keyboard shortcuts).
        /// </summary>
        public void RefreshFromBrush()
        {
            if (_brush == null) return;
            _radiusSlider.SetValueWithoutNotify(_brush.radius);
            _strengthSlider.SetValueWithoutNotify(_brush.strength);
            _falloffSlider.SetValueWithoutNotify(_brush.falloff);
            UpdateOpButtons();
        }

        private Slider CreateSlider(string label, float min, float max, float defaultValue)
        {
            var slider = new Slider(label, min, max);
            slider.value = defaultValue;
            slider.style.width = 140;
            slider.style.marginLeft = 8;
            slider.style.marginRight = 4;
            return slider;
        }

        private Button CreateOpButton(string label, BrushSettings.BrushOp op)
        {
            var btn = new Button(() =>
            {
                if (_brush != null)
                {
                    _brush.operation = op;
                    UpdateOpButtons();
                }
            });
            btn.text = label;
            btn.style.marginLeft = 2;
            btn.style.marginRight = 2;
            return btn;
        }

        private void UpdateOpButtons()
        {
            if (_brush == null) return;

            SetButtonSelected(_addBtn, _brush.operation == BrushSettings.BrushOp.Add);
            SetButtonSelected(_subBtn, _brush.operation == BrushSettings.BrushOp.Subtract);
            SetButtonSelected(_smoothBtn, _brush.operation == BrushSettings.BrushOp.Smooth);
        }

        private void SetButtonSelected(Button btn, bool selected)
        {
            if (selected)
            {
                btn.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
                btn.style.color = Color.white;
            }
            else
            {
                btn.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
                btn.style.color = new Color(0.7f, 0.7f, 0.7f);
            }
        }
    }
}
#endif

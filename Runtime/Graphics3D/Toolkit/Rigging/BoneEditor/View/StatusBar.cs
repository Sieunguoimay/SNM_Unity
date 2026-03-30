#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// VisualElement status bar showing: current mode name, bone count, vertex count,
    /// and unpainted vertex count (as a warning).
    /// </summary>
    public class StatusBar : VisualElement
    {
        private Label _modeLabel;
        private Label _boneCountLabel;
        private Label _vertCountLabel;
        private Label _unpaintedLabel;

        public StatusBar()
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.paddingTop = 3;
            style.paddingBottom = 3;
            style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            style.borderTopWidth = 1;
            style.borderTopColor = new Color(0.1f, 0.1f, 0.1f);

            _modeLabel = CreateLabel("Mode: ---");
            _modeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _modeLabel.style.marginRight = 16;
            Add(_modeLabel);

            _boneCountLabel = CreateLabel("Bones: 0");
            _boneCountLabel.style.marginRight = 16;
            Add(_boneCountLabel);

            _vertCountLabel = CreateLabel("Verts: 0");
            _vertCountLabel.style.marginRight = 16;
            Add(_vertCountLabel);

            _unpaintedLabel = CreateLabel("");
            _unpaintedLabel.style.color = new Color(1f, 0.7f, 0.2f);
            Add(_unpaintedLabel);
        }

        /// <summary>
        /// Updates the status bar from the current document state.
        /// </summary>
        public void UpdateFromDocument(RigDocument doc, string modeName)
        {
            if (doc == null)
            {
                _modeLabel.text = "Mode: ---";
                _boneCountLabel.text = "Bones: 0";
                _vertCountLabel.text = "Verts: 0";
                _unpaintedLabel.text = "";
                return;
            }

            _modeLabel.text = "Mode: " + (modeName ?? "---");
            _boneCountLabel.text = "Bones: " + (doc.bones != null ? doc.bones.Count : 0);

            int vertCount = doc.sourceMesh != null ? doc.sourceMesh.vertexCount : 0;
            _vertCountLabel.text = "Verts: " + vertCount;

            int unpainted = doc.GetUnpaintedVertexCount();
            if (unpainted > 0)
            {
                _unpaintedLabel.text = "!! " + unpainted + " unpainted";
                _unpaintedLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                _unpaintedLabel.text = "";
                _unpaintedLabel.style.display = DisplayStyle.None;
            }
        }

        private Label CreateLabel(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 11;
            return label;
        }
    }
}
#endif

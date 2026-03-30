#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// VisualElement that displays the bone hierarchy as indented labels with select buttons.
    /// Single-click selects a bone. Double-click enables inline rename via a TextField.
    /// Shows bone name and index.
    /// </summary>
    public class BoneTreeView : VisualElement
    {
        private RigDocument _doc;
        private ScrollView _scrollView;
        private int _renamingIndex = -1;
        private int[] _influenceCounts;

        public event Action<int> OnBoneSelected;

        public BoneTreeView()
        {
            style.flexGrow = 1f;
            style.minWidth = 200;

            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.style.flexGrow = 1f;
            Add(_scrollView);
        }

        /// <summary>
        /// Sets the document and refreshes the tree display.
        /// </summary>
        public void SetDocument(RigDocument doc)
        {
            _doc = doc;
            Rebuild();
        }

        /// <summary>
        /// Rebuilds the entire tree from the current document state.
        /// </summary>
        public void Rebuild()
        {
            _scrollView.Clear();
            _renamingIndex = -1;

            if (_doc == null || _doc.bones == null || _doc.bones.Count == 0)
            {
                _influenceCounts = null;
                var emptyLabel = new Label("No bones. Ctrl+Click in scene to create.");
                emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                emptyLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                emptyLabel.style.paddingLeft = 8;
                emptyLabel.style.paddingTop = 8;
                _scrollView.Add(emptyLabel);
                return;
            }

            // Cache influence counts (#6)
            CacheInfluenceCounts();

            // Build tree by iterating bones and indenting by hierarchy depth
            for (int i = 0; i < _doc.bones.Count; i++)
            {
                AddBoneRow(i);
            }
        }

        private void CacheInfluenceCounts()
        {
            int boneCount = _doc.bones.Count;
            _influenceCounts = new int[boneCount];

            if (_doc.vertexWeights == null) return;

            for (int v = 0; v < _doc.vertexWeights.Length; v++)
            {
                for (int b = 0; b < boneCount; b++)
                {
                    if (_doc.vertexWeights[v].GetWeight(b) > 0.001f)
                        _influenceCounts[b]++;
                }
            }
        }

        private void AddBoneRow(int boneIndex)
        {
            var bone = _doc.bones[boneIndex];
            int depth = GetBoneDepth(boneIndex);
            bool isSelected = (boneIndex == _doc.selectedBoneIndex);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 8 + depth * 16;
            row.style.paddingTop = 2;
            row.style.paddingBottom = 2;
            row.style.paddingRight = 4;

            if (isSelected)
            {
                row.style.backgroundColor = new Color(0.24f, 0.37f, 0.58f, 0.5f);
            }

            // Color indicator
            var colorDot = new VisualElement();
            colorDot.style.width = 10;
            colorDot.style.height = 10;
            colorDot.style.borderTopLeftRadius = 5;
            colorDot.style.borderTopRightRadius = 5;
            colorDot.style.borderBottomLeftRadius = 5;
            colorDot.style.borderBottomRightRadius = 5;
            colorDot.style.backgroundColor = bone.displayColor;
            colorDot.style.marginRight = 6;
            row.Add(colorDot);

            // Index label
            var indexLabel = new Label($"[{boneIndex}]");
            indexLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            indexLabel.style.width = 30;
            indexLabel.style.fontSize = 10;
            row.Add(indexLabel);

            if (_renamingIndex == boneIndex)
            {
                // Inline rename TextField
                var textField = new TextField();
                textField.value = bone.name;
                textField.style.flexGrow = 1f;
                textField.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        CommitRename(boneIndex, textField.value);
                    }
                    else if (evt.keyCode == KeyCode.Escape)
                    {
                        _renamingIndex = -1;
                        Rebuild();
                    }
                });
                textField.RegisterCallback<FocusOutEvent>(_ =>
                {
                    CommitRename(boneIndex, textField.value);
                });
                row.Add(textField);

                // Focus the text field after it is attached
                textField.RegisterCallback<AttachToPanelEvent>(_ =>
                {
                    textField.schedule.Execute(() => textField.Focus()).ExecuteLater(10);
                });
            }
            else
            {
                // Bone name label (clickable)
                var nameLabel = new Label(bone.name);
                nameLabel.style.flexGrow = 1f;
                nameLabel.style.color = isSelected ? Color.yellow : Color.white;

                int capturedIndex = boneIndex;

                // Single click: select
                nameLabel.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.clickCount == 2)
                    {
                        // Double-click: rename
                        _renamingIndex = capturedIndex;
                        Rebuild();
                    }
                    else
                    {
                        SelectBone(capturedIndex);
                    }
                });

                row.Add(nameLabel);
            }

            // Influence count label (#6)
            if (_influenceCounts != null && boneIndex < _influenceCounts.Length)
            {
                var countLabel = new Label($"{_influenceCounts[boneIndex]} vtx");
                countLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                countLabel.style.fontSize = 9;
                countLabel.style.marginLeft = 4;
                countLabel.style.marginRight = 4;
                countLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                row.Add(countLabel);
            }

            // Hierarchy indicator
            if (bone.parentIndex >= 0 && bone.parentIndex < _doc.bones.Count)
            {
                var parentLabel = new Label($"< {_doc.bones[bone.parentIndex].name}");
                parentLabel.style.color = new Color(0.4f, 0.4f, 0.4f);
                parentLabel.style.fontSize = 9;
                parentLabel.style.marginLeft = 8;
                row.Add(parentLabel);
            }

            _scrollView.Add(row);
        }

        private void SelectBone(int index)
        {
            if (_doc != null)
            {
                UndoHelper.Record(_doc, "Select Bone");
                _doc.selectedBoneIndex = index;
            }
            OnBoneSelected?.Invoke(index);
            Rebuild();
        }

        private void CommitRename(int index, string newName)
        {
            if (_doc != null && index >= 0 && index < _doc.bones.Count)
            {
                if (!string.IsNullOrWhiteSpace(newName) && newName != _doc.bones[index].name)
                {
                    UndoHelper.Record(_doc, "Rename Bone");
                    _doc.bones[index].name = newName;
                }
            }
            _renamingIndex = -1;
            Rebuild();
        }

        private int GetBoneDepth(int boneIndex)
        {
            int depth = 0;
            int current = boneIndex;
            int maxDepth = _doc.bones.Count; // Safety limit
            while (current >= 0 && depth < maxDepth)
            {
                int parent = _doc.bones[current].parentIndex;
                if (parent < 0) break;
                depth++;
                current = parent;
            }
            return depth;
        }
    }
}
#endif

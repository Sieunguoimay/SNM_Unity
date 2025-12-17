using System;
using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.GPUSkinning;
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneToolUI : IDisposable
    {
        private readonly RuntimeBoneCollection boneCollection;
        private readonly Mesh mesh;

        private BoneSelectorUI[] _boneSelectors;
        private VerticesSelectorUI _verticesSelector;

        public IReadOnlyList<BoneSelectorUI> BoneSelectors => _boneSelectors;
        public VerticesSelectorUI VerticesSelector => _verticesSelector;
        public IReadOnlyList<Vector3> AllVertices => mesh.vertices;

        public event Action OnBoneSelectorsChanged;
        public event Action OnVerticesSelectorChanged;

        public BoneToolUI(RuntimeBoneCollection boneCollection, Mesh mesh)
        {
            this.boneCollection = boneCollection;
            this.mesh = mesh;
            this.boneCollection.OnBonesChanged += BoneCollection_OnBonesChanged;

            UpdateBoneSelectors();
        }

        public void Dispose()
        {
            this.boneCollection.OnBonesChanged -= BoneCollection_OnBonesChanged;
        }

        private void BoneCollection_OnBonesChanged()
        {
            UpdateBoneSelectors();
        }

        private void UpdateBoneSelectors()
        {
            _boneSelectors = GenerateBoneSelectors(boneCollection.Bones);
            _boneSelectors.FirstOrDefault()?.Select();
            OnBoneSelectorsChanged?.Invoke();
        }

        public BoneSelectorUI[] GenerateBoneSelectors(IReadOnlyList<RuntimeBone> bones)
        {
            return bones.Select(bone => new BoneSelectorUI(onSelected: () =>
            {
                _verticesSelector = GenerateVerticesSelector(bone);
                OnVerticesSelectorChanged?.Invoke();
            }, onUnselected: () =>
            {
                _verticesSelector = null;
                OnVerticesSelectorChanged?.Invoke();
            })).ToArray();
        }

        public VerticesSelectorUI GenerateVerticesSelector(RuntimeBone bone)
        {
            var modifier = new BoneModifier(bone);
            return new VerticesSelectorUI(mesh, onSelected: (vIndex) =>
            {
                modifier.AddVertex(vIndex, 1f);
            }, onUnselected: (vIndex) =>
            {
                modifier.RemoveVertex(vIndex);
            }, bone.vertices?.Select(v => v.index).ToArray() ?? Array.Empty<int>());
        }

        public void AddNewBone()
        {
            boneCollection.SetBones(boneCollection.Bones.Append(new RuntimeBone()).ToArray());
        }
    }


    public class VerticesSelectorUI
    {
        private readonly Mesh mesh;
        private readonly Action<int> onSelected;
        private readonly Action<int> onUnselected;
        private readonly HashSet<int> selectedHashSet;

        public IReadOnlyList<Vector3> AllVertices => mesh.vertices;

        public VerticesSelectorUI(
            Mesh mesh,
            Action<int> onSelected,
            Action<int> onUnselected,
            int[] selectedVertices)
        {
            this.mesh = mesh;
            this.onSelected = onSelected;
            this.onUnselected = onUnselected;
            selectedHashSet = new HashSet<int>(selectedVertices);
        }

        public void Select(int vertex)
        {
            selectedHashSet.Add(vertex);
            onSelected?.Invoke(vertex);
        }

        public void Unselect(int vertex)
        {
            selectedHashSet.Remove(vertex);
            onUnselected?.Invoke(vertex);
        }

        public bool IsVertexSelected(int vertexIndex)
        {
            return selectedHashSet.Contains(vertexIndex);
        }
    }

    public class BoneSelectorUI
    {
        private readonly Action onSelected;
        private readonly Action onUnselected;
        private bool _isSelected;

        public bool IsSelected => _isSelected;
        public Action<BoneSelectorUI> _onIsSelectedChangedCallback;

        public BoneSelectorUI(
            Action onSelected,
            Action onUnselected)
        {
            this.onSelected = onSelected;
            this.onUnselected = onUnselected;
        }

        public void Select()
        {
            _isSelected = true;
            _onIsSelectedChangedCallback?.Invoke(this);
            onSelected?.Invoke();
        }

        public void Unselect()
        {
            _isSelected = false;
            _onIsSelectedChangedCallback?.Invoke(this);
            onUnselected?.Invoke();
        }

        public void SetIsSelectedChangeCallback(Action<BoneSelectorUI> callback)
        {
            _onIsSelectedChangedCallback = callback;
        }
    }
}
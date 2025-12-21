using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class VerticesSelectionTool
    {
        private readonly HashSet<int> selectedHashSet = new();
        private readonly List<int> selectedList = new();
        private readonly Mesh mesh;
        private bool _isActive;
        private Action _callback;

        public IReadOnlyList<Vector3> AllVertices => mesh.vertices;
        public IReadOnlyList<int> SelectedVertices => selectedList;
        public bool IsActive => _isActive;

        public VerticesSelectionTool(Mesh mesh)
        {
            this.mesh = mesh;
        }

        public void Select(int vertex)
        {
            MarkVertexAsSelected(vertex);
        }

        public void Unselect(int vertex)
        {
            MarkVertexAsUnselected(vertex);
        }

        public bool IsVertexSelected(int vertexIndex)
        {
            return selectedHashSet.Contains(vertexIndex);
        }

        public void SetIsActive(bool active)
        {
            _isActive = active;
        }

        public void MarkVertexAsUnselected(int vertex)
        {
            selectedList.Remove(vertex);
            selectedHashSet.Remove(vertex);
            _callback?.Invoke();
        }

        public void MarkVertexAsSelected(int vertex)
        {
            selectedList.Add(vertex);
            selectedHashSet.Add(vertex);
            _callback?.Invoke();
        }

        public void ClearMarks()
        {
            selectedList.Clear();
            selectedHashSet.Clear();
        }

        public void SetDirtyCallback(Action callback)
        {
            _callback = callback;
        }
    }
}
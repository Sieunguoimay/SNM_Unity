using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class VerticesSelectionTool
    {
        private readonly HashSet<int> selectedHashSet = new();
        private readonly Vector3[] vertices;
        private bool _isActive;
        private Action _callback;

        public IReadOnlyList<Vector3> AllVertices => vertices;
        public IEnumerable<int> SelectedVertices => selectedHashSet;
        public bool IsActive => _isActive;

        public VerticesSelectionTool(Vector3[] vertices)
        {
            this.vertices = vertices;
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
            selectedHashSet.Remove(vertex);
            _callback?.Invoke();
        }

        public void MarkVertexAsSelected(int vertex)
        {
            selectedHashSet.Add(vertex);
            _callback?.Invoke();
        }

        public void ClearMarks()
        {
            selectedHashSet.Clear();
        }

        public void SetDirtyCallback(Action callback)
        {
            _callback = callback;
        }
    }
}
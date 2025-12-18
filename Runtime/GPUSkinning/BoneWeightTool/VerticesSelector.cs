using System.Collections.Generic;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class VerticesSelector
    {
        private readonly HashSet<int> selectedHashSet = new();
        private readonly Vector3[] vertices;
        private BoneModifier _boneModifier;
        private bool _isActive;

        public IReadOnlyList<Vector3> AllVertices => vertices;
        public bool IsActive => _isActive;

        public VerticesSelector(Vector3[] vertices)
        {
            this.vertices = vertices;
        }

        public void Select(int vertex)
        {
            selectedHashSet.Add(vertex);
            _boneModifier?.AddVertex(vertex, 1f);
        }

        public void Unselect(int vertex)
        {
            selectedHashSet.Remove(vertex);
            _boneModifier?.RemoveVertex(vertex);
        }

        public bool IsVertexSelected(int vertexIndex)
        {
            return selectedHashSet.Contains(vertexIndex);
        }

        public void SetIsActive(bool active)
        {
            _isActive = active;
        }

        public void SetBoneModifier(BoneModifier boneModifier)
        {
            selectedHashSet.Clear();
            _boneModifier = boneModifier;

            if (_boneModifier != null)
            {
                for (var i = 0; i < vertices.Length; i++)
                {
                    if (_boneModifier.ContainsVertex(i))
                    {
                        selectedHashSet.Add(i);
                    }
                }
            }
        }

    }
}
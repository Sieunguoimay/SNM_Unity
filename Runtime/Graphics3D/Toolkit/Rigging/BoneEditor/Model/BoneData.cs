#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// Serializable bone definition: name, parent index, bind pose matrix, and display color.
    /// </summary>
    [Serializable]
    public class BoneData
    {
        public string name;
        public int parentIndex = -1;
        public Matrix4x4 bindpose = Matrix4x4.identity;
        public Color displayColor = Color.cyan;

        public BoneData() { }

        public BoneData(string name, int parentIndex, Matrix4x4 bindpose, Color displayColor)
        {
            this.name = name;
            this.parentIndex = parentIndex;
            this.bindpose = bindpose;
            this.displayColor = displayColor;
        }
    }
}
#endif

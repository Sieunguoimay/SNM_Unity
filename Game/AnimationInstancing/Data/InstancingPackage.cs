using UnityEngine;

namespace Snm.AnimationInstancing
{
    public class InstancingPackage
    {
        public int instancingCount;
        public MaterialPropertyBlock propertyBlock;

        public Matrix4x4[] worldMatrixArray;
        public float[] frameIndexArray;
        public float[] preFrameIndexArray;
        public float[] transitionProgressArray;

        public static readonly int InstancingPackageSize = 200;

        public InstancingPackage(int instancingCount)
        {
            this.instancingCount = instancingCount;
            propertyBlock = new MaterialPropertyBlock();
            worldMatrixArray = new Matrix4x4[InstancingPackageSize];
            frameIndexArray = new float[InstancingPackageSize];
            preFrameIndexArray = new float[InstancingPackageSize];
            transitionProgressArray = new float[InstancingPackageSize];
        }
    }
}
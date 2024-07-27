using UnityEngine;

namespace AnimationInstancing_v2
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

        public static InstancingPackage CreateInstancingPackage(int instancingCount)
        {
            return new InstancingPackage()
            {
                instancingCount = instancingCount,
                propertyBlock = new MaterialPropertyBlock(),
                worldMatrixArray = new Matrix4x4[InstancingPackageSize],
                frameIndexArray = new float[InstancingPackageSize],
                preFrameIndexArray = new float[InstancingPackageSize],
                transitionProgressArray = new float[InstancingPackageSize],
            };
        }
    }
}
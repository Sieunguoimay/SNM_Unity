using System.Collections.Generic;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class ClonedMaterialBlock
    {
        public Material[] clonedMaterials;
#if UNITY_EDITOR
        public int totalInstancingCount;
#endif
        public int instanceCountPerPackage;
        private readonly List<InstancingPackage> packageStack;

        private int _topPackageIndex = 0;
        public IReadOnlyList<InstancingPackage> PackageStack => packageStack;
        public InstancingPackage TopPackage => packageStack[_topPackageIndex];

        public ClonedMaterialBlock(Material[] clonedMaterials)
        {
            this.clonedMaterials = clonedMaterials;
            packageStack = new List<InstancingPackage>() { new(1) };
            instanceCountPerPackage = InstancingPackage.InstancingPackageSize;
        }

        public int NextInstanceIndex()
        {
            Debug.Assert(_topPackageIndex < packageStack.Count);

            if (TopPackage.instancingCount >= instanceCountPerPackage)
            {
                _topPackageIndex++;

                if (_topPackageIndex >= packageStack.Count)
                {
                    packageStack.Add(new InstancingPackage(1));
                }
                else
                {
                    TopPackage.instancingCount = 1;
                }
            }
            else
            {
                TopPackage.instancingCount++;
            }

            return TopPackage.instancingCount - 1;
        }

        public void ResetStack()
        {
            _topPackageIndex = 0;
        }
    }
}
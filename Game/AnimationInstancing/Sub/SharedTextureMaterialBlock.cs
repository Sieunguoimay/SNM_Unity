using System.Collections.Generic;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class ClonedMaterialBlock
    {
        public int instanceCountPerPackage;
        public Material[] clonedMaterials;
        private List<InstancingPackage> packageStack;

        private int _topPackageIndex = 0;
        public IReadOnlyList<InstancingPackage> PackageStack => packageStack;
        public InstancingPackage TopPackage => packageStack[_topPackageIndex];

        public static ClonedMaterialBlock Create(Material[] clonedMaterials)
        {
            var b = new ClonedMaterialBlock()
            {
                clonedMaterials = clonedMaterials,
                packageStack = new List<InstancingPackage>(),
                instanceCountPerPackage = InstancingPackage.InstancingPackageSize
            };

            b.packageStack
                .Add(InstancingPackage.CreateInstancingPackage(1));
            return b;
        }

        public int NextInstanceIndex()
        {
            Debug.Assert(_topPackageIndex < packageStack.Count);

            if (TopPackage.instancingCount >= instanceCountPerPackage)
            {
                _topPackageIndex++;

                if (_topPackageIndex >= packageStack.Count)
                {
                    packageStack.Add(InstancingPackage.CreateInstancingPackage(1));
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
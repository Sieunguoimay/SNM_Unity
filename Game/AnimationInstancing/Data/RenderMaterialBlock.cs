using System.Collections.Generic;
using UnityEngine;

namespace SNM_Unity.AnimationInstancing
{
    public class RenderMaterialBlock
    {
        private readonly List<InstancingPackage> packageStack = new() { new(1) };

        private int _topPackageIndex = 0;

        public Material[] Materials { get; }
#if UNITY_EDITOR
        public int totalInstancingCount;
#endif
        public int InstanceCountPerPackage { get; }

        public InstancingPackage TopPackage => packageStack[_topPackageIndex];
        public int PackageStackCount => packageStack.Count;

        public RenderMaterialBlock(Material[] materials)
        {
            Materials = materials;
            InstanceCountPerPackage = InstancingPackage.InstancingPackageSize;
        }

        public int NextInstanceIndex()
        {
            Debug.Assert(_topPackageIndex < packageStack.Count);

            if (TopPackage.instancingCount >= InstanceCountPerPackage)
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

        public InstancingPackage GetPackage(int index)
        {
            return packageStack[index];
        }
    }
}
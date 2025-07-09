using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Snm.AnimationInstancing
{
    public class RenderMaterialBlockGroup
    {
        public IReadOnlyList<RenderMaterialBlock> RenderMaterialBlocks { get; }
#if UNITY_EDITOR
        public IReadOnlyList<Material> OriginalSharedMaterials { get; }
#endif

        public RenderMaterialBlockGroup(IEnumerable<RenderMaterialBlock> renderMaterialBlocks, IReadOnlyList<Material> sharedMaterials)
        {
            RenderMaterialBlocks = renderMaterialBlocks.ToArray();
#if UNITY_EDITOR
            OriginalSharedMaterials = sharedMaterials;
#endif
        }
    }
}
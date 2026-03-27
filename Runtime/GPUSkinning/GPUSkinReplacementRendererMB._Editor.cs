#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    public partial class GPUSkinReplacementRendererMB
    {
        private void OnValidate()
        {
            if (gpuSkinningShader == null)
                gpuSkinningShader = AssetDatabase.LoadAssetAtPath<Shader>(
                    "Assets/SNM_Unity/Runtime/GPUSkinning/Shader/GPUSkin.shader");
            if (!isActiveAndEnabled) return;
            TryDestroy();
            TryCreate();
        }
    }
}
#endif

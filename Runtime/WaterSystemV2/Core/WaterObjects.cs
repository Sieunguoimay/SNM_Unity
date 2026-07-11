using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// Destroy that works in both play mode and edit mode ([ExecuteAlways]
    /// means teardown can run in either).
    /// </summary>
    public static class WaterObjects
    {
        public static void Destroy(Object obj)
        {
            if (obj == null) return;

            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }
    }
}

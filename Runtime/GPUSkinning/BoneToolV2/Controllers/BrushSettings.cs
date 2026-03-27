#if UNITY_EDITOR
namespace Snm.GPUSkinning.BoneToolV2
{
    /// <summary>
    /// Shared brush configuration for weight painting.
    /// </summary>
    public class BrushSettings
    {
        public enum BrushOp
        {
            Add,
            Subtract,
            Smooth
        }

        public float radius = 0.1f;
        public float strength = 0.5f;
        public float falloff = 0.5f;
        public BrushOp operation = BrushOp.Add;
    }
}
#endif

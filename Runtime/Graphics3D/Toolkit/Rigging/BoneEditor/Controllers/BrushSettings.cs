#if UNITY_EDITOR
using UnityEditor;

namespace Snm.Graphics3D.Rigging
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

        private const string PrefKeyRadius = "BoneToolV2.BrushRadius";
        private const string PrefKeyStrength = "BoneToolV2.BrushStrength";
        private const string PrefKeyFalloff = "BoneToolV2.BrushFalloff";

        public static void SaveToPrefs(BrushSettings settings)
        {
            if (settings == null) return;
            EditorPrefs.SetFloat(PrefKeyRadius, settings.radius);
            EditorPrefs.SetFloat(PrefKeyStrength, settings.strength);
            EditorPrefs.SetFloat(PrefKeyFalloff, settings.falloff);
        }

        public static BrushSettings LoadFromPrefs()
        {
            var settings = new BrushSettings();
            if (EditorPrefs.HasKey(PrefKeyRadius))
                settings.radius = EditorPrefs.GetFloat(PrefKeyRadius, 0.1f);
            if (EditorPrefs.HasKey(PrefKeyStrength))
                settings.strength = EditorPrefs.GetFloat(PrefKeyStrength, 0.5f);
            if (EditorPrefs.HasKey(PrefKeyFalloff))
                settings.falloff = EditorPrefs.GetFloat(PrefKeyFalloff, 0.5f);
            return settings;
        }
    }
}
#endif

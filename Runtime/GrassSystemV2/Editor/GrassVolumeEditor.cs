using UnityEditor;
using UnityEngine;

namespace Snm.GrassSystemV2.Editor
{
    /// <summary>
    /// Inspector for <see cref="GrassVolume"/>: fills the volume's footprint
    /// with grass according to its rules, baking straight into the active
    /// world's data (with undo). The volume itself never runs at runtime.
    /// </summary>
    [CustomEditor(typeof(GrassVolume))]
    [CanEditMultipleObjects]
    public class GrassVolumeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            var world = FindWorld();
            if (world == null || world.Data == null)
            {
                EditorGUILayout.HelpBox(
                    "No GrassWorld with data in the scene — add one before filling volumes.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Fill Volume", GUILayout.Height(28f)))
            {
                foreach (var volumeTarget in targets) Fill((GrassVolume)volumeTarget, world);
            }

            if (GUILayout.Button("Clear Volume Area", GUILayout.Height(28f)))
            {
                foreach (var volumeTarget in targets) ClearArea((GrassVolume)volumeTarget, world);
            }

            EditorGUILayout.EndHorizontal();
        }

        static GrassWorld FindWorld()
        {
            return GrassWorld.Instance != null
                ? GrassWorld.Instance
                : Object.FindFirstObjectByType<GrassWorld>();
        }

        static void Fill(GrassVolume volume, GrassWorld world)
        {
            var settings = new GrassPaintOps.ScatterSettings
            {
                TypeIndices = volume.typeIndices,
                GroundMask = volume.groundMask,
                MaxSlopeAngle = volume.maxSlopeAngle,
                RaycastOriginHeight = volume.raycastOriginHeight,
                RaycastMaxDistance = volume.raycastMaxDistance,
            };
            var rng = new System.Random(volume.randomSeed);

            int placed = 0;
            GrassPaintOps.WithUndo(world.Data, "Fill grass volume", () =>
            {
                placed = GrassPaintOps.ScatterInRect(
                    world.Data,
                    volume.transform.position,
                    volume.transform.eulerAngles.y,
                    volume.areaSize,
                    volume.density,
                    settings,
                    rng);
            });

            Debug.Log($"[GrassV2] '{volume.name}' filled: {placed:N0} blades placed.", volume);
        }

        static void ClearArea(GrassVolume volume, GrassWorld world)
        {
            // Clear a disc that covers the whole rectangle (simple and safe;
            // use the brush for precise erasing).
            float radius = volume.areaSize.magnitude * 0.5f;
            int removed = 0;
            GrassPaintOps.WithUndo(world.Data, "Clear grass volume area", () =>
            {
                removed = world.Data.RemoveInRadius(volume.transform.position, radius);
            });

            Debug.Log($"[GrassV2] '{volume.name}' area cleared: {removed:N0} blades removed.", volume);
        }
    }
}

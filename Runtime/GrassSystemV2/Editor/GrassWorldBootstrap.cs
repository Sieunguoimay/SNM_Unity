using UnityEditor;
using UnityEngine;

namespace Snm.GrassSystemV2.Editor
{
    /// <summary>
    /// One-click scene setup: creates a configured GrassWorld GameObject so a
    /// new prototype gets grass running without hunting through the component
    /// menu. Data is still painted per scene (never preset); this just stands up
    /// the world with a sensible default wind mood applied.
    /// </summary>
    public static class GrassWorldBootstrap
    {
        [MenuItem("GameObject/Snm/Grass World V2", false, 10)]
        static void CreateGrassWorld(MenuCommand command)
        {
            var go = new GameObject("Grass World V2");
            var world = go.AddComponent<GrassWorld>();

            // Default mood so a freshly created world already sways nicely.
            GrassWindPresets.Apply(world.Config, GrassWindPresets.All[2]); // "Meadow"

            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Grass World V2");
            Selection.activeObject = go;

            EditorUtility.DisplayDialog(
                "Grass World V2 created",
                "Next: click 'Create data asset' in the inspector, add a GrassType, " +
                "then 'Open Grass Painter' and paint. Prefab this GameObject to reuse " +
                "the tuning across prototypes.",
                "OK");
        }
    }
}

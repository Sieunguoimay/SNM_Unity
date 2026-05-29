#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

#pragma warning disable 618

namespace Snm.GrassSystem.EditorTools
{
    public static class GrassSystemMigrator
    {
        const string MenuPath = "Tools/Grass/Migrate GrassSystem to Patches";

        [MenuItem(MenuPath, true)]
        static bool ValidateMigrate()
        {
            return Selection.activeGameObject != null
                && Selection.activeGameObject.GetComponent<GrassSystem>() != null;
        }

        [MenuItem(MenuPath)]
        static void MigrateSelected()
        {
            var system = Selection.activeGameObject.GetComponent<GrassSystem>();
            var config = system.Config;
            if (config == null)
            {
                ShowError("GrassSystem has no config assigned.");
                return;
            }

            var existing = system.GetComponentsInChildren<GrassPatch>();
            if (existing.Length > 0)
            {
                ShowError($"GrassSystem already has {existing.Length} GrassPatch child(ren). Remove them first, or migrate a fresh GrassSystem.");
                return;
            }

            var plan = BuildPlan(config);
            if (plan.Count == 0)
            {
                ShowError("Config has no grid data (no mesh/material assigned, or empty layers array).");
                return;
            }

            if (!ShowPreview(plan)) return;

            Apply(system, plan);
            Debug.Log($"[GrassMigrator] Created {plan.Count} GrassPatch(es) under '{system.name}'. Grid fields on '{config.name}' were NOT modified — clear them manually after verifying.");
        }

        struct Entry
        {
            public string Name;
            public Mesh Mesh;
            public Material Material;
            public Texture2D PlacementMap;
            public int DensityChannel;
            public float DensityThreshold;
            public float MinScale;
            public float MaxScale;
            public int RandomSeed;
            public Vector2 AreaSize;
            public Vector2 CellSpacing;
        }

        static List<Entry> BuildPlan(GrassSystemConfig config)
        {
            var list = new List<Entry>();

            var gridSize = config.placementMap != null
                ? new Vector2Int(config.placementMap.width, config.placementMap.height)
                : config.gridSize;

            // +epsilon absorbs float rounding so FloorToInt doesn't drop a cell
            var areaSize = new Vector2(
                gridSize.x * config.cellSpacing.x + 0.0001f,
                gridSize.y * config.cellSpacing.y + 0.0001f);

            if (config.HasLayers)
            {
                foreach (var layer in config.layers)
                {
                    if (layer == null || layer.mesh == null || layer.material == null) continue;
                    var name = string.IsNullOrEmpty(layer.name) ? "Layer" : layer.name;
                    list.Add(new Entry
                    {
                        Name = $"Grass Patch - {name}",
                        Mesh = layer.mesh,
                        Material = layer.material,
                        PlacementMap = config.placementMap,
                        DensityChannel = layer.densityChannel,
                        DensityThreshold = layer.densityThreshold,
                        MinScale = layer.minScale,
                        MaxScale = layer.maxScale,
                        RandomSeed = Mathf.RoundToInt(layer.yawRandomSeed * 1000f),
                        AreaSize = areaSize,
                        CellSpacing = config.cellSpacing,
                    });
                }
            }
            else if (config.grassMesh != null && config.grassMaterial != null)
            {
                list.Add(new Entry
                {
                    Name = "Grass Patch (Migrated)",
                    Mesh = config.grassMesh,
                    Material = config.grassMaterial,
                    PlacementMap = config.placementMap,
                    DensityChannel = 0,
                    DensityThreshold = config.densityThreshold,
                    MinScale = config.minScale,
                    MaxScale = config.maxScale,
                    RandomSeed = 0,
                    AreaSize = areaSize,
                    CellSpacing = config.cellSpacing,
                });
            }

            return list;
        }

        static bool ShowPreview(List<Entry> plan)
        {
            var msg = new StringBuilder();
            msg.AppendLine($"Will create {plan.Count} GrassPatch child(ren):");
            msg.AppendLine();
            foreach (var e in plan)
            {
                msg.AppendLine($"• {e.Name}");
                msg.AppendLine($"    mesh      = {e.Mesh.name}");
                msg.AppendLine($"    material  = {e.Material.name}");
                msg.AppendLine($"    area      = {e.AreaSize.x:0.##} x {e.AreaSize.y:0.##}");
                msg.AppendLine($"    spacing   = {e.CellSpacing.x:0.##} x {e.CellSpacing.y:0.##}");
                msg.AppendLine($"    channel   = {e.DensityChannel} (threshold {e.DensityThreshold:0.##})");
                msg.AppendLine($"    scale     = {e.MinScale:0.##}..{e.MaxScale:0.##}");
                msg.AppendLine();
            }
            msg.AppendLine("Patches will use snapToTerrain=false and jitter=0 to preserve grid-path semantics.");
            msg.AppendLine("Per-blade yaw will differ from the grid baseline (System.Random vs spatial hash).");
            msg.AppendLine("The GrassSystemConfig asset will NOT be modified.");

            return EditorUtility.DisplayDialog("Migrate GrassSystem to Patches", msg.ToString(), "Apply", "Cancel");
        }

        static void Apply(GrassSystem system, List<Entry> plan)
        {
            Undo.SetCurrentGroupName("Migrate GrassSystem to Patches");
            int group = Undo.GetCurrentGroup();

            foreach (var entry in plan)
            {
                var go = new GameObject(entry.Name);
                Undo.RegisterCreatedObjectUndo(go, "Create GrassPatch");
                go.transform.SetParent(system.transform, worldPositionStays: false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                var patch = Undo.AddComponent<GrassPatch>(go);
                patch.mesh = entry.Mesh;
                patch.material = entry.Material;
                patch.placementMap = entry.PlacementMap;
                patch.densityChannel = entry.DensityChannel;
                patch.densityThreshold = entry.DensityThreshold;
                patch.minScale = entry.MinScale;
                patch.maxScale = entry.MaxScale;
                patch.randomSeed = entry.RandomSeed;
                patch.areaSize = entry.AreaSize;
                patch.cellSpacing = entry.CellSpacing;
                patch.jitter = 0f;
                patch.snapToTerrain = false;
                patch.minYaw = 0f;
                patch.maxYaw = 360f;

                EditorUtility.SetDirty(patch);
            }

            EditorUtility.SetDirty(system);
            EditorUtility.SetDirty(system.gameObject);
            Undo.CollapseUndoOperations(group);
        }

        static void ShowError(string msg) => EditorUtility.DisplayDialog("Grass Migrator", msg, "OK");
    }
}
#endif

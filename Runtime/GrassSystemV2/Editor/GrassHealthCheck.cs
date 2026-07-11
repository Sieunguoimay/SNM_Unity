using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Snm.GrassSystemV2.Editor
{
    /// <summary>
    /// Configuration validation with one-click fixes, shown at the top of the
    /// GrassWorld inspector. Kills the classic "no grass and no idea why"
    /// debugging session — every known misconfiguration is detected and named.
    /// </summary>
    public static class GrassHealthCheck
    {
        public enum Severity { Info, Warning, Error }

        public class Issue
        {
            public Severity Severity;
            public string Message;
            public string FixLabel;   // null = no automatic fix
            public Action Fix;
        }

        public const string ShaderName = "Snm/GrassV2";

        public static List<Issue> Run(GrassWorld world)
        {
            var issues = new List<Issue>();
            var serialized = new SerializedObject(world);
            var data = world.Data;

            // --- World-level ---
            if (data == null)
            {
                issues.Add(new Issue
                {
                    Severity = Severity.Error,
                    Message = "No GrassWorldData assigned — the world has nothing to draw.",
                    FixLabel = "Create data asset",
                    Fix = () => CreateDataAsset(serialized),
                });
                return issues; // everything below needs data
            }

            var worlds = UnityEngine.Object.FindObjectsByType<GrassWorld>(FindObjectsSortMode.None);
            if (worlds.Length > 1)
            {
                issues.Add(new Issue
                {
                    Severity = Severity.Warning,
                    Message = $"{worlds.Length} GrassWorld components in the scene. Only one is supported; extras stay idle.",
                });
            }

            if (world.Config.chunkSize != data.chunkSize)
            {
                issues.Add(new Issue
                {
                    Severity = Severity.Error,
                    Message = $"Chunk size mismatch: world config says {world.Config.chunkSize}, data was painted with {data.chunkSize}.",
                    FixLabel = "Use data's chunk size",
                    Fix = () =>
                    {
                        serialized.FindProperty("config.chunkSize").floatValue = data.chunkSize;
                        serialized.ApplyModifiedProperties();
                    },
                });
            }

            // --- Types ---
            if (data.types.Length == 0)
            {
                issues.Add(new Issue
                {
                    Severity = Severity.Error,
                    Message = "Data has no grass types. Add at least one GrassType asset to the data's Types list.",
                });
            }

            for (int i = 0; i < data.types.Length; i++)
            {
                var type = data.types[i];
                if (type == null)
                {
                    issues.Add(new Issue { Severity = Severity.Error, Message = $"Types[{i}] is empty." });
                    continue;
                }
                if (type.meshLod0 == null)
                {
                    issues.Add(new Issue { Severity = Severity.Error, Message = $"'{type.name}' has no LOD0 mesh." });
                }
                if (type.material == null)
                {
                    issues.Add(new Issue { Severity = Severity.Error, Message = $"'{type.name}' has no material." });
                }
                else if (type.material.shader == null || type.material.shader.name != ShaderName)
                {
                    var material = type.material;
                    issues.Add(new Issue
                    {
                        Severity = Severity.Error,
                        Message = $"'{type.name}' material uses shader '{material.shader?.name}' — grass only renders with '{ShaderName}'.",
                        FixLabel = "Switch shader",
                        Fix = () =>
                        {
                            Undo.RecordObject(material, "Switch grass shader");
                            material.shader = Shader.Find(ShaderName);
                            EditorUtility.SetDirty(material);
                        },
                    });
                }
            }

            // --- Platform / tier ---
            if (world.Config.tierMode == GrassRenderTierMode.ForceGpuDriven && !SystemInfo.supportsComputeShaders)
            {
                issues.Add(new Issue
                {
                    Severity = Severity.Warning,
                    Message = "GPU-driven tier is forced but this device reports no compute shader support. It will fall back to Simple.",
                });
            }

            // --- Camera ---
            var cameraProperty = serialized.FindProperty("cameraOverride");
            if (cameraProperty.objectReferenceValue == null && Camera.main == null)
            {
                issues.Add(new Issue
                {
                    Severity = Severity.Warning,
                    Message = "No camera override and no camera tagged MainCamera — nothing will be culled or drawn in play mode.",
                });
            }

            // --- Budgets & content ---
            if (data.TotalInstanceCount == 0)
            {
                issues.Add(new Issue
                {
                    Severity = Severity.Info,
                    Message = "No grass painted yet. Select the GrassWorld and use the Grass Painter tool (toolbar) or a GrassVolume.",
                });
            }

            if (world.Config.canvasWorldSize < world.Config.chunkSize)
            {
                issues.Add(new Issue
                {
                    Severity = Severity.Warning,
                    Message = "Interaction canvas is smaller than one chunk — trample/effects will only cover a tiny area around the camera.",
                });
            }

            int largestTypeCount = LargestVisibleTypeCount(data, world.Config);
            if (largestTypeCount > world.Config.maxVisibleInstancesPerType)
            {
                issues.Add(new Issue
                {
                    Severity = Severity.Warning,
                    Message = $"A type can have up to ~{largestTypeCount:N0} blades within draw distance, above the GPU-tier budget " +
                              $"({world.Config.maxVisibleInstancesPerType:N0}). Far blades may drop out; raise the budget if you see holes.",
                });
            }

            return issues;
        }

        /// <summary>Worst-case visible blades of any single type (rough upper bound).</summary>
        static int LargestVisibleTypeCount(GrassWorldData data, GrassWorldConfig config)
        {
            // Conservative: sum every chunk that could sit inside the draw radius
            // simultaneously is overkill to compute exactly; approximate with the
            // densest chunks by type times the visible-chunk count cap.
            var perTypeMax = new Dictionary<byte, int>();
            foreach (var chunk in data.Chunks)
            {
                foreach (var range in chunk.typeRanges)
                {
                    perTypeMax.TryGetValue(range.typeIndex, out int current);
                    perTypeMax[range.typeIndex] = Mathf.Max(current, range.count);
                }
            }

            float chunksAcross = config.maxDrawDistance * 2f / Mathf.Max(config.chunkSize, 0.001f);
            int visibleChunkCap = Mathf.CeilToInt(chunksAcross * chunksAcross * 0.6f); // frustum sees ~60% of the disc square

            int worst = 0;
            foreach (var kvp in perTypeMax) worst = Mathf.Max(worst, kvp.Value * visibleChunkCap);
            return worst;
        }

        static void CreateDataAsset(SerializedObject serialized)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Grass World Data", "GrassWorldData", "asset",
                "Where should the painted grass data live?");
            if (string.IsNullOrEmpty(path)) return;

            var data = ScriptableObject.CreateInstance<GrassWorldData>();
            var world = (GrassWorld)serialized.targetObject;
            data.chunkSize = world.Config.chunkSize;
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();

            serialized.FindProperty("data").objectReferenceValue = data;
            serialized.ApplyModifiedProperties();
        }
    }
}

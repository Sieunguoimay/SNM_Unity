using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Snm.GrassSystemV2.Editor
{
    /// <summary>
    /// Shared editing operations behind the brush tool and the volume filler:
    /// scatter blades onto colliders, erase, all with undo + live refresh.
    /// Ground raycasts happen HERE, at authoring time — the runtime never
    /// raycasts (V1 raycast per blade on every enable, stalling big patches).
    /// </summary>
    public static class GrassPaintOps
    {
        /// <summary>Scatter parameters for one operation.</summary>
        public struct ScatterSettings
        {
            public int[] TypeIndices;
            public LayerMask GroundMask;
            public float MaxSlopeAngle;
            public float RaycastOriginHeight;
            public float RaycastMaxDistance;
        }

        /// <summary>
        /// Scatters <paramref name="count"/> blades in a world-space XZ disc.
        /// Returns how many actually landed (missed raycasts and steep ground are skipped).
        /// </summary>
        public static int ScatterInDisc(
            GrassWorldData data,
            Vector3 center,
            float radius,
            int count,
            ScatterSettings settings,
            System.Random rng)
        {
            var instances = new List<GrassInstance>(count);

            for (int i = 0; i < count; i++)
            {
                // Uniform disc sampling (sqrt for even area distribution).
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float distance = Mathf.Sqrt((float)rng.NextDouble()) * radius;
                var samplePosition = center + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);

                if (!TryPlaceBlade(data, samplePosition, settings, rng, out var instance)) continue;
                instances.Add(instance);
            }

            data.AddInstances(instances);
            return instances.Count;
        }

        /// <summary>Scatters blades over an oriented rectangle (GrassVolume fill).</summary>
        public static int ScatterInRect(
            GrassWorldData data,
            Vector3 center,
            float yawDegrees,
            Vector2 areaSize,
            float density,
            ScatterSettings settings,
            System.Random rng)
        {
            int count = Mathf.CeilToInt(areaSize.x * areaSize.y * density);
            var rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            var instances = new List<GrassInstance>(count);

            for (int i = 0; i < count; i++)
            {
                var local = new Vector3(
                    ((float)rng.NextDouble() - 0.5f) * areaSize.x,
                    0f,
                    ((float)rng.NextDouble() - 0.5f) * areaSize.y);
                var samplePosition = center + rotation * local;

                if (!TryPlaceBlade(data, samplePosition, settings, rng, out var instance)) continue;
                instances.Add(instance);
            }

            data.AddInstances(instances);
            return instances.Count;
        }

        static bool TryPlaceBlade(
            GrassWorldData data,
            Vector3 samplePosition,
            ScatterSettings settings,
            System.Random rng,
            out GrassInstance instance)
        {
            instance = default;

            var origin = samplePosition + Vector3.up * settings.RaycastOriginHeight;
            if (!Physics.Raycast(origin, Vector3.down, out var hit, settings.RaycastMaxDistance, settings.GroundMask))
                return false;

            if (Vector3.Angle(hit.normal, Vector3.up) > settings.MaxSlopeAngle)
                return false;

            var typeIndices = settings.TypeIndices is { Length: > 0 } ? settings.TypeIndices : new[] { 0 };
            int typeIndex = typeIndices[rng.Next(typeIndices.Length)];
            if (typeIndex < 0 || typeIndex >= data.types.Length || data.types[typeIndex] == null)
                return false;

            var type = data.types[typeIndex];
            float scale = Mathf.Lerp(type.scaleRange.x, type.scaleRange.y, (float)rng.NextDouble());

            instance = GrassInstance.Create(
                hit.point,
                (float)rng.NextDouble() * Mathf.PI * 2f,
                scale,
                (byte)typeIndex,
                (byte)rng.Next(256));
            return true;
        }

        /// <summary>Wraps a mutation with undo registration + dirty + live world refresh.</summary>
        public static void WithUndo(GrassWorldData data, string operationName, System.Action mutate)
        {
            Undo.RegisterCompleteObjectUndo(data, operationName);
            mutate();
            EditorUtility.SetDirty(data);
            RefreshWorld();
        }

        public static void RefreshWorld()
        {
            if (GrassWorld.Instance != null) GrassWorld.Instance.RebuildChunks();
            SceneView.RepaintAll();
        }
    }
}

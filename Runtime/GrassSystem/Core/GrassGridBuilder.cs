using System.Collections.Generic;
using Snm.SurfaceInteraction;
using UnityEngine;

#pragma warning disable 618

namespace Snm.GrassSystem
{
    [System.Obsolete("GrassGridBuilder is part of the deprecated grid path. Use GrassPatch children and GrassPatchCollector instead. Will be removed in a future release.")]
    public static class GrassGridBuilder
    {
        public struct Result
        {
            public Matrix4x4[] Matrices;
            public Matrix4x4[][] LayerMatrices;
            public SurfaceCanvas Canvas;
            public Bounds WorldBounds;
        }

        public static Result Build(GrassSystemConfig config, Transform transform)
        {
            int sizeX, sizeZ;

            if (config.placementMap != null)
            {
                sizeX = config.placementMap.width;
                sizeZ = config.placementMap.height;
            }
            else
            {
                sizeX = config.gridSize.x;
                sizeZ = config.gridSize.y;
            }

            float spacingX = config.cellSpacing.x;
            float spacingZ = config.cellSpacing.y;

            float totalWidth = (sizeX - 1) * spacingX;
            float totalDepth = (sizeZ - 1) * spacingZ;
            Vector3 pivotOffset = new(-totalWidth * 0.5f, 0f, -totalDepth * 0.5f);

            var worldCenter = transform.TransformPoint(pivotOffset + new Vector3(totalWidth * 0.5f, 0f, totalDepth * 0.5f));
            var worldCorner = transform.TransformPoint(pivotOffset + new Vector3(totalWidth, 0f, totalDepth));
            var worldOrigin = transform.TransformPoint(pivotOffset);
            var worldSize = new Vector2(
                Mathf.Abs(worldCorner.x - worldOrigin.x),
                Mathf.Abs(worldCorner.z - worldOrigin.z));

            var canvas = new SurfaceCanvas
            {
                Position = worldCenter,
                Rotation = Quaternion.identity,
                Size = worldSize
            };
            var worldBounds = new Bounds(worldCenter, new Vector3(worldSize.x, 10f, worldSize.y));

            Color32[] pixels = config.placementMap != null
                ? config.placementMap.GetPixels32()
                : null;

            Matrix4x4[] matrices;
            Matrix4x4[][] layerMatrices;

            if (config.HasLayers)
            {
                layerMatrices = new Matrix4x4[config.layers.Length][];
                for (int i = 0; i < config.layers.Length; i++)
                {
                    layerMatrices[i] = BuildLayerGrid(
                        sizeX, sizeZ, spacingX, spacingZ, pivotOffset, pixels,
                        config.layers[i], transform);
                }
                matrices = layerMatrices.Length > 0 ? layerMatrices[0] : System.Array.Empty<Matrix4x4>();
            }
            else
            {
                matrices = BuildDefaultGrid(
                    sizeX, sizeZ, spacingX, spacingZ, pivotOffset, pixels,
                    config, transform);
                layerMatrices = null;
            }

            return new Result
            {
                Matrices = matrices,
                LayerMatrices = layerMatrices,
                Canvas = canvas,
                WorldBounds = worldBounds
            };
        }

        static Matrix4x4[] BuildDefaultGrid(
            int sizeX, int sizeZ, float spacingX, float spacingZ,
            Vector3 pivotOffset, Color32[] pixels,
            GrassSystemConfig config, Transform transform)
        {
            var list = new List<Matrix4x4>(sizeX * sizeZ);

            for (int z = 0; z < sizeZ; z++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    float density = 1f;
                    float yawNorm = -1f;
                    float scaleNorm = 0.5f;

                    if (pixels != null)
                    {
                        var c = pixels[z * sizeX + x];
                        density = c.r / 255f;
                        if (density < config.densityThreshold) continue;
                        yawNorm = c.g / 255f;
                        scaleNorm = c.b / 255f;
                    }

                    var localPos = new Vector3(x * spacingX, 0f, z * spacingZ) + pivotOffset;
                    var worldPos = transform.TransformPoint(localPos);

                    float yaw;
                    if (yawNorm >= 0f)
                        yaw = yawNorm * 360f;
                    else
                    {
                        uint hash = (uint)(x * 73856093 ^ z * 19349663);
                        yaw = (hash % 3600) / 10f;
                    }

                    var rot = transform.rotation * Quaternion.Euler(0f, yaw, 0f);

                    float scale = pixels != null
                        ? Mathf.Lerp(config.minScale, config.maxScale, scaleNorm)
                        : 1f;

                    list.Add(Matrix4x4.TRS(worldPos, rot, Vector3.one * scale));
                }
            }

            return list.ToArray();
        }

        static Matrix4x4[] BuildLayerGrid(
            int sizeX, int sizeZ, float spacingX, float spacingZ,
            Vector3 pivotOffset, Color32[] pixels,
            GrassLayerConfig layer, Transform transform)
        {
            var list = new List<Matrix4x4>(sizeX * sizeZ);

            for (int z = 0; z < sizeZ; z++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    if (pixels != null)
                    {
                        var c = pixels[z * sizeX + x];
                        float density = GetChannel(c, layer.densityChannel) / 255f;
                        if (density < layer.densityThreshold) continue;
                    }

                    var localPos = new Vector3(x * spacingX, 0f, z * spacingZ) + pivotOffset;
                    var worldPos = transform.TransformPoint(localPos);

                    uint hash = (uint)(x * 73856093 ^ z * 19349663 ^ (int)(layer.yawRandomSeed * 7919));
                    float yaw = (hash % 3600) / 10f;
                    var rot = transform.rotation * Quaternion.Euler(0f, yaw, 0f);

                    uint scaleHash = (uint)(x * 45161 ^ z * 37913 ^ (int)(layer.yawRandomSeed * 12289));
                    float scaleNorm = (scaleHash % 1000) / 999f;
                    float scale = Mathf.Lerp(layer.minScale, layer.maxScale, scaleNorm);

                    list.Add(Matrix4x4.TRS(worldPos, rot, Vector3.one * scale));
                }
            }

            return list.ToArray();
        }

        static float GetChannel(Color32 c, int channel)
        {
            return channel switch
            {
                0 => c.r,
                1 => c.g,
                2 => c.b,
                3 => c.a,
                _ => c.r
            };
        }
    }
}

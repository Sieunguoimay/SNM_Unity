using System;
using UnityEngine;

namespace Snm.GrassSystemV2
{
    /// <summary>
    /// Owns one runtime material clone per grass type with the type's look
    /// parameters baked in. Cloning keeps the authored material asset
    /// untouched (same rationale as V1's renderer, done once here instead of
    /// per feature).
    /// </summary>
    public sealed class GrassTypeMaterials : IDisposable
    {
        public static class ShaderIds
        {
            public static readonly int ColorA = Shader.PropertyToID("_GrassColorA");
            public static readonly int ColorB = Shader.PropertyToID("_GrassColorB");
            public static readonly int AoStrength = Shader.PropertyToID("_GrassAoStrength");
            public static readonly int AoPower = Shader.PropertyToID("_GrassAoPower");
            public static readonly int SwayAmount = Shader.PropertyToID("_GrassSwayAmount");
            public static readonly int SwayFrequency = Shader.PropertyToID("_GrassSwayFrequency");
            public static readonly int BladeHeight = Shader.PropertyToID("_GrassBladeHeight");
            public static readonly int SpringParams = Shader.PropertyToID("_GrassSpringParams");
            public static readonly int Instances = Shader.PropertyToID("_GrassInstances");
            public static readonly int BaseIndex = Shader.PropertyToID("_GrassBaseIndex");
        }

        readonly Material[] _materials;

        public GrassTypeMaterials(GrassType[] types, GrassWorldConfig config)
        {
            _materials = new Material[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (type == null || !type.IsValid) continue;

                // HideAndDontSave keeps the editor from leaking clones into the
                // scene/asset graph across domain reloads.
                var material = new Material(type.material) { hideFlags = HideFlags.HideAndDontSave };
                material.SetColor(ShaderIds.ColorA, type.colorA);
                material.SetColor(ShaderIds.ColorB, type.colorB);
                material.SetFloat(ShaderIds.AoStrength, type.aoStrength);
                material.SetFloat(ShaderIds.AoPower, type.aoPower);
                material.SetFloat(ShaderIds.SwayAmount, type.swayAmount);
                material.SetFloat(ShaderIds.SwayFrequency, type.swayFrequency);
                material.SetFloat(ShaderIds.BladeHeight, type.BladeHeight);
                material.SetVector(ShaderIds.SpringParams,
                    new Vector4(config.springFrequency, config.springDamping, config.springAmplitude, 0f));
                _materials[i] = material;
            }
        }

        /// <summary>Material clone for a type index, or null when the type is invalid.</summary>
        public Material Get(int typeIndex)
        {
            return typeIndex >= 0 && typeIndex < _materials.Length ? _materials[typeIndex] : null;
        }

        public void Dispose()
        {
            foreach (var material in _materials)
            {
                if (material == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                else UnityEngine.Object.DestroyImmediate(material);
            }
            Array.Clear(_materials, 0, _materials.Length);
        }
    }
}

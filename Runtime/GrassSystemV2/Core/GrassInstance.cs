using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Snm.GrassSystemV2
{
    /// <summary>
    /// One grass blade, packed to 20 bytes. The CPU layout matches the GPU
    /// StructuredBuffer layout exactly (see GrassV2Common.hlsl), so chunk
    /// arrays upload with a single SetData and no conversion pass.
    ///
    /// Layout:
    ///   Position               float3  (12 bytes)
    ///   PackedYawScale         uint    low 16 = yaw [0..2pi), high 16 = scale * 1000
    ///   PackedTypeSeedFlags    uint    low 8 = type index, next 8 = seed, high 16 = flags
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct GrassInstance
    {
        /// <summary>Size in bytes of one instance on both CPU and GPU.</summary>
        public const int Stride = 20;

        /// <summary>Blade has been cut/destroyed. Shader collapses it to zero scale.</summary>
        public const ushort FlagCut = 1 << 0;

        public Vector3 Position;
        public uint PackedYawScale;
        public uint PackedTypeSeedFlags;

        const float YawToUnorm16 = 65535f / (Mathf.PI * 2f);
        const float Unorm16ToYaw = (Mathf.PI * 2f) / 65535f;
        const float ScaleQuantum = 0.001f; // scale stored as thousandths, max 65.535

        /// <summary>Yaw rotation around Y in radians, [0..2pi). Quantized to 16 bits.</summary>
        public float Yaw
        {
            get => (PackedYawScale & 0xFFFF) * Unorm16ToYaw;
            set
            {
                float wrapped = Mathf.Repeat(value, Mathf.PI * 2f);
                PackedYawScale = (PackedYawScale & 0xFFFF0000)
                               | (uint)Mathf.RoundToInt(wrapped * YawToUnorm16);
            }
        }

        /// <summary>Uniform scale. Quantized to 0.001 steps, max 65.535.</summary>
        public float Scale
        {
            get => (PackedYawScale >> 16) * ScaleQuantum;
            set
            {
                uint q = (uint)Mathf.Clamp(Mathf.RoundToInt(value / ScaleQuantum), 0, 0xFFFF);
                PackedYawScale = (PackedYawScale & 0x0000FFFF) | (q << 16);
            }
        }

        /// <summary>Index into <see cref="GrassWorldData.types"/>.</summary>
        public byte TypeIndex
        {
            get => (byte)(PackedTypeSeedFlags & 0xFF);
            set => PackedTypeSeedFlags = (PackedTypeSeedFlags & 0xFFFFFF00) | value;
        }

        /// <summary>Per-blade random byte driving color variation and sway phase.</summary>
        public byte Seed
        {
            get => (byte)((PackedTypeSeedFlags >> 8) & 0xFF);
            set => PackedTypeSeedFlags = (PackedTypeSeedFlags & 0xFFFF00FF) | ((uint)value << 8);
        }

        public ushort Flags
        {
            get => (ushort)(PackedTypeSeedFlags >> 16);
            set => PackedTypeSeedFlags = (PackedTypeSeedFlags & 0x0000FFFF) | ((uint)value << 16);
        }

        public bool IsCut
        {
            get => (Flags & FlagCut) != 0;
            set => Flags = value ? (ushort)(Flags | FlagCut) : (ushort)(Flags & ~FlagCut);
        }

        public static GrassInstance Create(Vector3 position, float yaw, float scale, byte typeIndex, byte seed)
        {
            var instance = new GrassInstance { Position = position };
            instance.Yaw = yaw;
            instance.Scale = scale;
            instance.TypeIndex = typeIndex;
            instance.Seed = seed;
            return instance;
        }
    }
}

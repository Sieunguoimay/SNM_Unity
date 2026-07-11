using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.GrassSystemV2
{
    /// <summary>
    /// The sliding world-space canvas all grass interaction flows through.
    /// Two small ping-ponged RTs follow the camera focus (snapped to the texel
    /// grid so content never swims):
    ///
    ///   Bend    — RG push direction, B hold, A fading energy (trample + spring)
    ///   Effects — R burn, G freeze, B tint
    ///
    /// Gameplay stamps are queued from anywhere (<see cref="QueueBend"/>,
    /// <see cref="QueueEffect"/>) and flushed once per frame by
    /// <see cref="GrassWorld"/>. Content slides with the canvas; anything that
    /// leaves the covered area is forgotten (cutting is the exception — it
    /// lives in instance flags, not here, and is truly persistent).
    /// </summary>
    public sealed class GrassInteractionCanvas : IDisposable
    {
        const int MaxStamps = 64; // must match MAX_STAMPS in GrassV2Canvas.shader

        static class Ids
        {
            public static readonly int PrevTex = Shader.PropertyToID("_PrevTex");
            public static readonly int ShiftUV = Shader.PropertyToID("_ShiftUV");
            public static readonly int CanvasRect = Shader.PropertyToID("_CanvasRect");
            public static readonly int DeltaTime = Shader.PropertyToID("_DeltaTime");
            public static readonly int FadeSpeed = Shader.PropertyToID("_FadeSpeed");
            public static readonly int HoldDecay = Shader.PropertyToID("_HoldDecay");
            public static readonly int FreezeTintDecay = Shader.PropertyToID("_FreezeTintDecay");
            public static readonly int DirectionLock = Shader.PropertyToID("_DirectionLock");
            public static readonly int StampSoftness = Shader.PropertyToID("_StampSoftness");
            public static readonly int StampCount = Shader.PropertyToID("_StampCount");
            public static readonly int Stamps = Shader.PropertyToID("_Stamps");
            public static readonly int StampParams = Shader.PropertyToID("_StampParams");
            public static readonly int GrassBendMap = Shader.PropertyToID("_GrassBendMap");
            public static readonly int GrassEffectMap = Shader.PropertyToID("_GrassEffectMap");
            public static readonly int GrassCanvasRect = Shader.PropertyToID("_GrassCanvasRect");
        }

        struct Stamp
        {
            public Vector2 Position;
            public float Radius;
            public float Strength;
            public Vector2 DirectionOrChannel;
            public float Core; // flat-core fraction, resolved (never negative here)
        }

        readonly GrassWorldConfig _config;
        readonly Material _material;
        readonly List<Stamp> _bendStamps = new(MaxStamps);
        readonly List<Stamp> _effectStamps = new(MaxStamps);
        readonly Vector4[] _stampVectors = new Vector4[MaxStamps];
        readonly Vector4[] _stampParamVectors = new Vector4[MaxStamps];

        RenderTexture _bendA, _bendB, _effectsA, _effectsB;
        bool _swapped;
        Vector2 _worldMin;
        bool _initialized;

        public Vector2 WorldMin => _worldMin;
        public float WorldSize => _config.canvasWorldSize;
        public RenderTexture BendTexture => _swapped ? _bendB : _bendA;
        public RenderTexture EffectsTexture => _swapped ? _effectsB : _effectsA;

        public GrassInteractionCanvas(GrassWorldConfig config)
        {
            _config = config;

            var shader = Shader.Find("Hidden/Snm/GrassV2Canvas");
            if (shader == null)
            {
                Debug.LogError("[GrassV2] Shader 'Hidden/Snm/GrassV2Canvas' not found. " +
                               "Is the GrassSystemV2/Shaders folder intact?");
                return;
            }
            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

            _bendA = CreateTexture("GrassV2 Bend A");
            _bendB = CreateTexture("GrassV2 Bend B");
            _effectsA = CreateTexture("GrassV2 Effects A");
            _effectsB = CreateTexture("GrassV2 Effects B");
        }

        RenderTexture CreateTexture(string name)
        {
            var texture = new RenderTexture(
                _config.canvasResolution, _config.canvasResolution, 0,
                RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            texture.Create();

            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = previous;
            return texture;
        }

        /// <summary>
        /// Queues a bend stamp (trample). <paramref name="coreFraction"/> is the
        /// 0..1 portion of the radius pressed fully flat (0.5 = inner half).
        /// </summary>
        public void QueueBend(Vector3 worldPosition, Vector2 direction, float radius, float strength, float coreFraction = 0.5f)
        {
            if (_bendStamps.Count >= MaxStamps) return; // overflow: oldest wins, extras dropped this frame
            _bendStamps.Add(new Stamp
            {
                Position = new Vector2(worldPosition.x, worldPosition.z),
                Radius = radius,
                Strength = Mathf.Clamp01(strength),
                DirectionOrChannel = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right,
                Core = Mathf.Clamp(coreFraction, 0f, 0.95f),
            });
        }

        /// <summary>Queues an area effect stamp. Flushed on the next canvas update.</summary>
        public void QueueEffect(GrassEffect effect, Vector3 worldPosition, float radius, float strength, float coreFraction = 0.5f)
        {
            if (_effectStamps.Count >= MaxStamps) return;
            _effectStamps.Add(new Stamp
            {
                Position = new Vector2(worldPosition.x, worldPosition.z),
                Radius = radius,
                Strength = Mathf.Clamp01(strength),
                DirectionOrChannel = new Vector2((int)effect, 0f),
                Core = Mathf.Clamp(coreFraction, 0f, 0.95f),
            });
        }

        /// <summary>Re-centers on the camera focus, applies decay, flushes stamps, publishes globals.</summary>
        public void Update(float deltaTime, Vector3 focusWorldPosition)
        {
            if (_material == null) return;

            // Snap the canvas origin to the texel grid so sliding never blurs content.
            float texelWorldSize = _config.canvasWorldSize / _config.canvasResolution;
            float half = _config.canvasWorldSize * 0.5f;
            var targetMin = new Vector2(
                Mathf.Round((focusWorldPosition.x - half) / texelWorldSize) * texelWorldSize,
                Mathf.Round((focusWorldPosition.z - half) / texelWorldSize) * texelWorldSize);

            Vector2 shiftUV = _initialized
                ? (targetMin - _worldMin) / _config.canvasWorldSize
                : Vector2.zero;
            _worldMin = targetMin;
            _initialized = true;

            var canvasRect = new Vector4(_worldMin.x, _worldMin.y, _config.canvasWorldSize, _config.canvasWorldSize);
            _material.SetVector(Ids.ShiftUV, shiftUV);
            _material.SetVector(Ids.CanvasRect, canvasRect);
            _material.SetFloat(Ids.DeltaTime, deltaTime);
            _material.SetFloat(Ids.FadeSpeed, _config.bendFadeSpeed);
            _material.SetFloat(Ids.HoldDecay, 1f / Mathf.Max(_config.bendHoldTime, 0.001f));
            _material.SetVector(Ids.FreezeTintDecay, new Vector4(
                _config.freezeThawTime > 0f ? 1f / _config.freezeThawTime : 0f,
                0.1f, 0f, 0f));
            _material.SetFloat(Ids.DirectionLock, Mathf.Max(_config.directionLockAmount, 0.1f));
            _material.SetFloat(Ids.StampSoftness, Mathf.Max(_config.bendEdgeSoftness, 0.1f));

            var bendSource = _swapped ? _bendB : _bendA;
            var bendTarget = _swapped ? _bendA : _bendB;
            var effectsSource = _swapped ? _effectsB : _effectsA;
            var effectsTarget = _swapped ? _effectsA : _effectsB;

            UploadStamps(_bendStamps);
            _material.SetTexture(Ids.PrevTex, bendSource);
            Graphics.Blit(bendSource, bendTarget, _material, 0);

            UploadStamps(_effectStamps);
            _material.SetTexture(Ids.PrevTex, effectsSource);
            Graphics.Blit(effectsSource, effectsTarget, _material, 1);

            _swapped = !_swapped;
            _bendStamps.Clear();
            _effectStamps.Clear();

            Shader.SetGlobalTexture(Ids.GrassBendMap, BendTexture);
            Shader.SetGlobalTexture(Ids.GrassEffectMap, EffectsTexture);
            Shader.SetGlobalVector(Ids.GrassCanvasRect, canvasRect);
        }

        void UploadStamps(List<Stamp> stamps)
        {
            for (int i = 0; i < stamps.Count; i++)
            {
                var stamp = stamps[i];
                _stampVectors[i] = new Vector4(stamp.Position.x, stamp.Position.y, stamp.Radius, stamp.Strength);
                _stampParamVectors[i] = new Vector4(stamp.DirectionOrChannel.x, stamp.DirectionOrChannel.y, stamp.Core, 0f);
            }
            _material.SetInt(Ids.StampCount, stamps.Count);
            if (stamps.Count > 0)
            {
                _material.SetVectorArray(Ids.Stamps, _stampVectors);
                _material.SetVectorArray(Ids.StampParams, _stampParamVectors);
            }
        }

        public void Dispose()
        {
            ReleaseTexture(ref _bendA);
            ReleaseTexture(ref _bendB);
            ReleaseTexture(ref _effectsA);
            ReleaseTexture(ref _effectsB);

            if (_material != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_material);
                else UnityEngine.Object.DestroyImmediate(_material);
            }
        }

        static void ReleaseTexture(ref RenderTexture texture)
        {
            if (texture == null) return;
            texture.Release();
            if (Application.isPlaying) UnityEngine.Object.Destroy(texture);
            else UnityEngine.Object.DestroyImmediate(texture);
            texture = null;
        }
    }
}

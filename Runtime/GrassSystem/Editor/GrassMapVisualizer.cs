using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#pragma warning disable 618

namespace Snm.GrassSystem
{
    [RequireComponent(typeof(GrassSystem))]
    public class GrassMapVisualizer : MonoBehaviour
    {
        public enum MapChannel { RGBA, R, G, B, A }

        [Header("Map Overlays")]
        public bool showTrampleMap = true;
        public MapChannel trampleChannel = MapChannel.A;
        public bool showWindMap;
        public MapChannel windChannel = MapChannel.RGBA;
        [Range(0.05f, 1f)] public float opacity = 0.5f;
        public float heightOffset = 0.05f;

        [Header("Gizmos")]
        public bool showBounds = true;
        public bool showHeightPlanes = true;
        public bool showBladeMesh = true;
        public bool showBladePositions;

        GrassSystem _grassSystem;
        Material _overlayMat;
        Mesh _quadMesh;

        static readonly int ID_MainTex = Shader.PropertyToID("_MainTex");
        static readonly int ID_Opacity = Shader.PropertyToID("_Opacity");
        static readonly int ID_ChannelMask = Shader.PropertyToID("_ChannelMask");
        static readonly int ID_ScrollOffset = Shader.PropertyToID("_ScrollOffset");
        static readonly int ID_MapScale = Shader.PropertyToID("_MapScale");
        static readonly int ID_UseScroll = Shader.PropertyToID("_UseScroll");
        static readonly int ID_ShowAllChannels = Shader.PropertyToID("_ShowAllChannels");

        void OnEnable()
        {
            EnsureInitialized();
        }

        void EnsureInitialized()
        {
            if (_grassSystem == null)
                _grassSystem = GetComponent<GrassSystem>();
            if (_overlayMat == null)
            {
                var shader = Shader.Find("Hidden/Snm/GrassMapOverlay");
                if (shader != null)
                    _overlayMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            if (_quadMesh == null)
                _quadMesh = CreateQuad();
        }

        void OnDisable()
        {
            if (_overlayMat != null) DestroyImmediate(_overlayMat);
            if (_quadMesh != null) DestroyImmediate(_quadMesh);
            _overlayMat = null;
            _quadMesh = null;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            EnsureInitialized();
            if (_grassSystem == null) return;

            var config = _grassSystem.Config;
            var canvas = _grassSystem.Canvas;

            Vector3 center;
            Vector3 size;

            if (canvas != null)
            {
                center = canvas.Position;
                size = new Vector3(canvas.Size.x, 0f, canvas.Size.y);
            }
            else
            {
                float totalWidth = (config.gridSize.x - 1) * config.cellSpacing.x;
                float totalDepth = (config.gridSize.y - 1) * config.cellSpacing.y;
                center = transform.position;
                size = new Vector3(totalWidth, 0f, totalDepth);
            }

            // --- Bounds & height planes ---
            if (showBounds)
                DrawGroundPlane(center, size);

            if (showHeightPlanes)
            {
                float hx = size.x * 0.5f;
                float hz = size.z * 0.5f;
                var corners = new[]
                {
                    center + new Vector3(-hx, 0f, -hz),
                    center + new Vector3( hx, 0f, -hz),
                    center + new Vector3( hx, 0f,  hz),
                    center + new Vector3(-hx, 0f,  hz),
                };

                DrawHeightPlane(center, size, corners, config.bladeHeight,
                    new Color(0.3f, 0.9f, 0.3f, 0.1f), new Color(0.3f, 0.9f, 0.3f, 0.6f));
                DrawHeightPlane(center, size, corners, config.interactionHeight,
                    new Color(1f, 1f, 0.2f, 0.1f), new Color(1f, 1f, 0.2f, 0.6f));

                Handles.color = new Color(0.3f, 0.9f, 0.3f);
                Handles.Label(center + new Vector3(hx, config.bladeHeight, hz),
                    $"Blade Height: {config.bladeHeight:F2}");
                Handles.color = Color.yellow;
                Handles.Label(center + new Vector3(-hx, config.interactionHeight, hz),
                    $"Interaction Height: {config.interactionHeight:F2}");
            }

            // --- Blade mesh & positions ---
            if (showBladeMesh)
                DrawBladeMeshPreview(config, center, transform.rotation);

            if (showBladePositions)
                DrawBladePositions(config, _grassSystem.Matrices, transform);

            // --- Map overlays ---
            if (_overlayMat == null) return;

            var canvasForMaps = canvas;
            if (canvasForMaps == null)
            {
                canvasForMaps = new SurfaceInteraction.SurfaceCanvas
                {
                    Position = center,
                    Rotation = Quaternion.identity,
                    Size = new Vector2(size.x, size.z),
                };
            }

            if (showTrampleMap)
                DrawTrampleMap(canvasForMaps);

            if (showWindMap)
                DrawWindMap(canvasForMaps);
        }
#endif

        // ---- Map overlay drawing ----

        void DrawTrampleMap(SurfaceInteraction.SurfaceCanvas canvas)
        {
            var trample = _grassSystem.Trample;
            if (trample == null) return;

            var rt = trample.OutputTexture;
            if (rt == null) return;

            SetupMaterial(rt, trampleChannel, false);
            DrawQuad(canvas);
        }

        void DrawWindMap(SurfaceInteraction.SurfaceCanvas canvas)
        {
            var windCfg = _grassSystem.Config.wind;
            if (windCfg.windMap == null) return;

            SetupMaterial(windCfg.windMap, windChannel, true);

            _overlayMat.SetVector(ID_ScrollOffset, new Vector4(
                Time.time * windCfg.windScrollSpeed,
                Time.time * windCfg.windScrollSpeed, 0, 0));
            _overlayMat.SetVector(ID_MapScale, new Vector4(
                windCfg.windMapScale.x, windCfg.windMapScale.y, 0, 0));

            DrawQuad(canvas);
        }

        void SetupMaterial(Texture tex, MapChannel channel, bool useScroll)
        {
            _overlayMat.SetTexture(ID_MainTex, tex);
            _overlayMat.SetFloat(ID_Opacity, opacity);
            _overlayMat.SetFloat(ID_UseScroll, useScroll ? 1f : 0f);
            _overlayMat.SetFloat(ID_ShowAllChannels, channel == MapChannel.RGBA ? 1f : 0f);
            _overlayMat.SetVector(ID_ChannelMask, ChannelToMask(channel));
        }

        void DrawQuad(SurfaceInteraction.SurfaceCanvas canvas)
        {
            var center = canvas.Position + Vector3.up * heightOffset;
            var size = new Vector3(canvas.Size.x, canvas.Size.y, 1f);
            var matrix = Matrix4x4.TRS(center, Quaternion.Euler(90f, 0f, 0f), size);
            _overlayMat.SetPass(0);
            Graphics.DrawMeshNow(_quadMesh, matrix);
        }

        // ---- Gizmo drawing (merged from GrassSystemGizmoDrawer) ----

        static void DrawGroundPlane(Vector3 center, Vector3 size)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.15f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(center, size);
        }

        static void DrawHeightPlane(Vector3 center, Vector3 size, Vector3[] corners,
            float height, Color fillColor, Color wireColor)
        {
            var top = center + Vector3.up * height;
            Gizmos.color = fillColor;
            Gizmos.DrawCube(top, size);
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(top, size);
            foreach (var c in corners)
                Gizmos.DrawLine(c, c + Vector3.up * height);
        }

        static void DrawBladeMeshPreview(GrassSystemConfig config, Vector3 center, Quaternion rotation)
        {
            if (config.grassMesh == null) return;

            Gizmos.color = new Color(0.3f, 0.9f, 0.3f, 0.5f);
            Gizmos.DrawMesh(config.grassMesh, center, rotation);
            Gizmos.color = new Color(0f, 0.4f, 0f, 0.8f);
            Gizmos.DrawWireMesh(config.grassMesh, center, rotation);
        }

        static void DrawBladePositions(GrassSystemConfig config, Matrix4x4[] matrices, Transform transform)
        {
            if (matrices != null)
            {
                Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.8f);
                float dotSize = Mathf.Min(config.cellSpacing.x, config.cellSpacing.y) * 0.15f;
                for (int i = 0; i < matrices.Length; i++)
                    Gizmos.DrawSphere(matrices[i].GetPosition(), dotSize);
            }
            else
            {
                Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.6f);
                int sizeX = config.gridSize.x;
                int sizeZ = config.gridSize.y;
                float spX = config.cellSpacing.x;
                float spZ = config.cellSpacing.y;
                float tw = (sizeX - 1) * spX;
                float td = (sizeZ - 1) * spZ;
                Vector3 pivot = new(-tw * 0.5f, 0f, -td * 0.5f);
                float dotSize = Mathf.Min(spX, spZ) * 0.15f;
                for (int z = 0; z < sizeZ; z++)
                    for (int x = 0; x < sizeX; x++)
                    {
                        var localPos = new Vector3(x * spX, 0f, z * spZ) + pivot;
                        Gizmos.DrawSphere(transform.TransformPoint(localPos), dotSize);
                    }
            }
        }

        // ---- Utilities ----

        static Vector4 ChannelToMask(MapChannel ch) => ch switch
        {
            MapChannel.R => new Vector4(1, 0, 0, 0),
            MapChannel.G => new Vector4(0, 1, 0, 0),
            MapChannel.B => new Vector4(0, 0, 1, 0),
            MapChannel.A => new Vector4(0, 0, 0, 1),
            _ => new Vector4(1, 1, 1, 0),
        };

        static Mesh CreateQuad()
        {
            var mesh = new Mesh
            {
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0),
                    new Vector3( 0.5f, -0.5f, 0),
                    new Vector3( 0.5f,  0.5f, 0),
                    new Vector3(-0.5f,  0.5f, 0),
                },
                uv = new[]
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(1, 1),
                    new Vector2(0, 1),
                },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
            };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}

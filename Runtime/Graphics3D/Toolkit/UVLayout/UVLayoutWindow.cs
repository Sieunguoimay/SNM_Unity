#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.UVLayout
{
    public class UVLayoutWindow : EditorWindow
    {
        [SerializeField] Mesh targetMesh;
        [SerializeField] Mesh compareMesh; // for mesh comparison mode
        [SerializeField] UVLayoutSettings settings = new();

        // Cached render state
        Texture2D _preview;
        Texture2D _comparePreview;
        UVLayoutAnalyzer.UVStats _stats;
        bool _dirty = true;
        bool _meshNotReadable;

        // Analysis caches
        HashSet<int> _overlappingTris;
        HashSet<int> _outOfBoundsTris;
        List<List<int>> _islands;
        Color[] _islandColors;
        float[] _texelDensities;
        List<(Vector2 a, Vector2 b)> _seamEdges;
        float[,] _vertexDensityMap;
        float _vertexDensityMax;
        UVLayoutAnalyzer.LightmapValidation _lightmapValidation;
        bool _lightmapValidated;

        // Pan & zoom
        Vector2 _panOffset;
        float _zoom = 1f;
        bool _isPanning;
        Vector2 _panStart;

        // Snapshot history
        readonly List<(Texture2D tex, string label)> _snapshots = new();
        const int MaxSnapshots = 10;
        Vector2 _snapshotScroll;

        // Hovered island
        int _hoveredIsland = -1;

        // UI state
        bool _showDisplaySettings = true;
        bool _showExportSettings = true;
        bool _showAnalysis = true;
        bool _showSceneView;
        bool _showLightmap;
        bool _showSnapshots;
        bool _showAdvanced;
        Vector2 _scrollPos;

        // Mesh comparison mode
        bool _meshCompareMode;

        [MenuItem("Tools/Snm/3D Toolkit/UV/UV Layout Tool")]
        public static void Open()
        {
            var window = GetWindow<UVLayoutWindow>("UV Layout");
            window.minSize = new Vector2(600, 450);
        }

        void OnEnable()
        {
            _dirty = true;
            Selection.selectionChanged += OnSelectionChanged;
        }

        void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            UVLayoutSceneOverlay.Disable();
            DestroyPreview();
            DestroyComparePreview();
            ClearSnapshots();
        }

        void OnSelectionChanged() => Repaint();

        #region OnGUI

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // Left: preview area
            EditorGUILayout.BeginVertical();
            DrawPreviewArea();
            if (_showSnapshots)
                DrawSnapshotStrip();
            EditorGUILayout.EndVertical();

            // Right: controls panel
            EditorGUILayout.BeginVertical(GUILayout.Width(280));
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawMeshSelector();
            DrawDisplaySettings();
            DrawAdvancedVisualization();
            DrawExportSettings();
            DrawSceneViewSettings();
            DrawAnalysisSection();
            DrawLightmapValidation();
            DrawSnapshotSection();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Preview

        void DrawPreviewArea()
        {
            if (_dirty && targetMesh != null)
                RefreshPreview();

            var rect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (rect.width <= 1 || rect.height <= 1) return;

            // Background
            EditorGUI.DrawRect(rect, settings.transparentBackground
                ? new Color(0.15f, 0.15f, 0.15f) : settings.backgroundColor);

            // Handle pan & zoom input
            HandlePanZoom(rect);

            if (settings.compareMode || _meshCompareMode)
            {
                DrawComparisonView(rect);
                return;
            }

            if (_preview != null)
            {
                float size = Mathf.Min(rect.width, rect.height) - 8;
                size *= _zoom;
                var center = rect.center + _panOffset;
                var previewRect = new Rect(
                    center.x - size * 0.5f,
                    center.y - size * 0.5f,
                    size, size);

                GUI.BeginClip(rect);
                var clippedRect = new Rect(
                    previewRect.x - rect.x,
                    previewRect.y - rect.y,
                    previewRect.width, previewRect.height);
                GUI.DrawTexture(clippedRect, _preview, ScaleMode.ScaleToFit);

                // Island hover detection
                if (settings.colorByIsland && _islands != null)
                    HandleIslandHover(clippedRect, rect);

                GUI.EndClip();
            }
            else if (targetMesh != null && _meshNotReadable)
            {
                EditorGUI.LabelField(rect,
                    "Mesh is not readable — enable Read/Write in import settings",
                    EditorStyles.centeredGreyMiniLabel);
            }
            else if (targetMesh != null)
            {
                EditorGUI.LabelField(rect, "No UVs on this channel",
                    EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                EditorGUI.LabelField(rect, "Select a mesh to preview",
                    EditorStyles.centeredGreyMiniLabel);
            }

            // Zoom indicator
            if (_zoom != 1f)
            {
                var zoomRect = new Rect(rect.x + 4, rect.yMax - 20, 80, 16);
                EditorGUI.LabelField(zoomRect, $"Zoom: {_zoom:F1}x", EditorStyles.miniLabel);
            }
        }

        void DrawComparisonView(Rect rect)
        {
            float halfW = rect.width * 0.5f - 2;
            var leftRect = new Rect(rect.x, rect.y, halfW, rect.height);
            var rightRect = new Rect(rect.x + halfW + 4, rect.y, halfW, rect.height);

            // Divider
            EditorGUI.DrawRect(new Rect(rect.x + halfW, rect.y, 4, rect.height),
                new Color(0.2f, 0.2f, 0.2f));

            // Left: primary channel / mesh
            DrawPreviewInRect(leftRect, _preview,
                _meshCompareMode ? $"Mesh: {(targetMesh ? targetMesh.name : "none")}"
                                 : $"UV{settings.uvChannel}");

            // Right: compare channel / mesh
            DrawPreviewInRect(rightRect, _comparePreview,
                _meshCompareMode ? $"Mesh: {(compareMesh ? compareMesh.name : "none")}"
                                 : $"UV{settings.compareUVChannel}");
        }

        void DrawPreviewInRect(Rect rect, Texture2D tex, string label)
        {
            if (tex != null)
            {
                float size = Mathf.Min(rect.width, rect.height) - 4;
                var previewRect = new Rect(
                    rect.x + (rect.width - size) * 0.5f,
                    rect.y + (rect.height - size) * 0.5f,
                    size, size);
                GUI.DrawTexture(previewRect, tex, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.LabelField(rect, "No preview", EditorStyles.centeredGreyMiniLabel);
            }

            var labelRect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, 16);
            EditorGUI.LabelField(labelRect, label, EditorStyles.whiteBoldLabel);
        }

        void HandlePanZoom(Rect rect)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;

            // Scroll to zoom
            if (e.type == EventType.ScrollWheel)
            {
                float delta = -e.delta.y * 0.05f;
                _zoom = Mathf.Clamp(_zoom + delta * _zoom, 0.1f, 10f);
                e.Use();
                Repaint();
            }

            // Middle-click to pan
            if (e.type == EventType.MouseDown && e.button == 2)
            {
                _isPanning = true;
                _panStart = e.mousePosition - _panOffset;
                e.Use();
            }
            if (e.type == EventType.MouseDrag && _isPanning)
            {
                _panOffset = e.mousePosition - _panStart;
                e.Use();
                Repaint();
            }
            if (e.type == EventType.MouseUp && e.button == 2)
            {
                _isPanning = false;
                e.Use();
            }

            // Double-click to reset
            if (e.type == EventType.MouseDown && e.clickCount == 2 && e.button == 0)
            {
                _zoom = 1f;
                _panOffset = Vector2.zero;
                e.Use();
                Repaint();
            }
        }

        void HandleIslandHover(Rect previewRect, Rect clipRect)
        {
            if (_islands == null || _preview == null) return;
            var e = Event.current;
            var mousePos = e.mousePosition;

            if (!clipRect.Contains(mousePos + clipRect.position)) return;

            // Convert mouse to UV space
            float u = (mousePos.x - previewRect.x) / previewRect.width;
            float v = (mousePos.y - previewRect.y) / previewRect.height;
            v = 1f - v; // flip Y

            if (u < 0 || u > 1 || v < 0 || v > 1)
            {
                _hoveredIsland = -1;
                return;
            }

            // Find which island contains this UV point
            var uvs = UVLayoutAnalyzer.GetUVChannel(targetMesh, settings.uvChannel);
            var tris = targetMesh.triangles;
            _hoveredIsland = -1;

            for (int idx = 0; idx < _islands.Count; idx++)
            {
                foreach (int t in _islands[idx])
                {
                    int i0 = tris[t * 3], i1 = tris[t * 3 + 1], i2 = tris[t * 3 + 2];
                    if (i0 >= uvs.Length || i1 >= uvs.Length || i2 >= uvs.Length) continue;

                    if (PointInTriangle(new Vector2(u, v), uvs[i0], uvs[i1], uvs[i2]))
                    {
                        _hoveredIsland = idx;

                        // Tooltip
                        var tooltipRect = new Rect(mousePos.x + 10, mousePos.y - 20, 120, 20);
                        GUI.Label(tooltipRect, $"Island {idx} ({_islands[idx].Count} tris)",
                            EditorStyles.helpBox);
                        Repaint();
                        return;
                    }
                }
            }
        }

        static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross2D(b - a, p - a);
            float d2 = Cross2D(c - b, p - b);
            float d3 = Cross2D(a - c, p - c);
            return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
        }

        static float Cross2D(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        #endregion

        #region Refresh

        void RefreshPreview()
        {
            _dirty = false;
            DestroyPreview();
            DestroyComparePreview();
            _meshNotReadable = false;
            _stats = default;
            _overlappingTris = null;
            _outOfBoundsTris = null;
            _islands = null;
            _islandColors = null;
            _texelDensities = null;
            _seamEdges = null;
            _vertexDensityMap = null;
            _lightmapValidated = false;

            if (targetMesh == null) return;

            Mesh meshToRead = GetReadableMesh(targetMesh);
            if (meshToRead == null) { _meshNotReadable = true; return; }

            bool isTemp = meshToRead != targetMesh;

            _stats = UVLayoutAnalyzer.Analyze(meshToRead, settings.uvChannel);
            if (!_stats.HasUVs)
            {
                if (isTemp) DestroyImmediate(meshToRead);
                return;
            }

            // Compute analysis data based on enabled features
            if (settings.highlightOverlaps)
                _overlappingTris = UVLayoutAnalyzer.FindOverlappingTriangles(meshToRead, settings.uvChannel);
            if (settings.highlightOutOfBounds)
                _outOfBoundsTris = UVLayoutAnalyzer.GetOutOfBoundsTriangles(meshToRead, settings.uvChannel);
            if (settings.colorByIsland)
            {
                _islands = UVLayoutAnalyzer.GetIslands(meshToRead, settings.uvChannel);
                _islandColors = UVLayoutAnalyzer.GenerateIslandColors(_islands.Count);
            }
            if (settings.showTexelDensity)
                _texelDensities = UVLayoutAnalyzer.ComputeTexelDensity(meshToRead, settings.uvChannel);
            if (settings.showSeams)
                _seamEdges = UVLayoutAnalyzer.FindSeamEdges(meshToRead, settings.uvChannel);
            if (settings.showVertexDensity)
            {
                var uvs = UVLayoutAnalyzer.GetUVChannel(meshToRead, settings.uvChannel);
                int densityRes = Mathf.Min(256, settings.resolution);
                _vertexDensityMap = UVLayoutAnalyzer.ComputeVertexDensityMap(uvs, densityRes, settings.vertexDensityRadius);
                _vertexDensityMax = UVLayoutAnalyzer.FindMaxDensity(_vertexDensityMap, densityRes);
            }

            // Render primary
            _preview = UVLayoutRenderer.Render(new UVLayoutRenderer.RenderContext
            {
                Mesh = meshToRead,
                Settings = settings,
                OverlappingTris = _overlappingTris,
                OutOfBoundsTris = _outOfBoundsTris,
                Islands = _islands,
                IslandColors = _islandColors,
                TexelDensities = _texelDensities,
                SeamEdges = _seamEdges,
                VertexDensityMap = _vertexDensityMap,
                VertexDensityMax = _vertexDensityMax
            });

            // Render comparison
            if (settings.compareMode)
            {
                var compareSettings = JsonUtility.FromJson<UVLayoutSettings>(JsonUtility.ToJson(settings));
                compareSettings.uvChannel = settings.compareUVChannel;
                _comparePreview = UVLayoutRenderer.Render(meshToRead, compareSettings);
            }
            else if (_meshCompareMode && compareMesh != null)
            {
                var compareMeshRead = GetReadableMesh(compareMesh);
                if (compareMeshRead != null)
                {
                    _comparePreview = UVLayoutRenderer.Render(compareMeshRead, settings);
                    if (compareMeshRead != compareMesh) DestroyImmediate(compareMeshRead);
                }
            }

            // Update scene overlay
            if ((settings.checkerPatternScene || settings.texelDensityScene) && _targetTransformCache != null)
                UVLayoutSceneOverlay.Enable(meshToRead, _targetTransformCache, settings);

            if (isTemp) DestroyImmediate(meshToRead);
        }

        Transform _targetTransformCache;

        Mesh GetReadableMesh(Mesh mesh)
        {
            if (mesh.isReadable) return mesh;

            var copy = Instantiate(mesh);
            var uvs = UVLayoutAnalyzer.GetUVChannel(copy, settings.uvChannel);
            if (uvs.Length == 0)
            {
                DestroyImmediate(copy);
                return null;
            }
            return copy;
        }

        void DestroyPreview()
        {
            if (_preview != null) { DestroyImmediate(_preview); _preview = null; }
        }

        void DestroyComparePreview()
        {
            if (_comparePreview != null) { DestroyImmediate(_comparePreview); _comparePreview = null; }
        }

        void ClearSnapshots()
        {
            foreach (var (tex, _) in _snapshots)
                if (tex != null) DestroyImmediate(tex);
            _snapshots.Clear();
        }

        void MarkDirty() => _dirty = true;

        #endregion

        #region Controls: Mesh Selector

        void DrawMeshSelector()
        {
            EditorGUILayout.LabelField("Mesh", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            targetMesh = (Mesh)EditorGUILayout.ObjectField("Target Mesh", targetMesh, typeof(Mesh), false);
            if (EditorGUI.EndChangeCheck()) MarkDirty();

            // Mesh comparison toggle
            _meshCompareMode = EditorGUILayout.Toggle("Compare Meshes", _meshCompareMode);
            if (_meshCompareMode)
            {
                EditorGUI.BeginChangeCheck();
                compareMesh = (Mesh)EditorGUILayout.ObjectField("Compare Mesh", compareMesh, typeof(Mesh), false);
                if (EditorGUI.EndChangeCheck()) MarkDirty();
            }

            if (GUILayout.Button("Use Selection"))
            {
                var (mesh, transform) = GetMeshFromSelection();
                if (mesh != null)
                {
                    targetMesh = mesh;
                    _targetTransformCache = transform;
                    MarkDirty();
                }
            }

            // UV Channel
            EditorGUI.BeginChangeCheck();
            settings.uvChannel = EditorGUILayout.IntSlider("UV Channel", settings.uvChannel, 0, 7);
            if (EditorGUI.EndChangeCheck()) MarkDirty();

            // Compare channels
            settings.compareMode = EditorGUILayout.Toggle("Compare UV Channels", settings.compareMode);
            if (settings.compareMode)
            {
                EditorGUI.BeginChangeCheck();
                settings.compareUVChannel = EditorGUILayout.IntSlider("Compare Channel", settings.compareUVChannel, 0, 7);
                if (EditorGUI.EndChangeCheck()) MarkDirty();
            }

            EditorGUILayout.Space(4);
        }

        #endregion

        #region Controls: Display Settings

        void DrawDisplaySettings()
        {
            _showDisplaySettings = EditorGUILayout.Foldout(_showDisplaySettings, "Display Settings", true);
            if (!_showDisplaySettings) return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            settings.lineColor = EditorGUILayout.ColorField("Line Color", settings.lineColor);
            settings.backgroundColor = EditorGUILayout.ColorField("Background", settings.backgroundColor);
            settings.transparentBackground = EditorGUILayout.Toggle("Transparent BG", settings.transparentBackground);

            settings.showFill = EditorGUILayout.Toggle("Show Fill", settings.showFill);
            if (settings.showFill)
                settings.fillColor = EditorGUILayout.ColorField("Fill Color", settings.fillColor);

            settings.showGrid = EditorGUILayout.Toggle("Show Grid", settings.showGrid);
            if (settings.showGrid)
            {
                settings.gridColor = EditorGUILayout.ColorField("Grid Color", settings.gridColor);
                settings.gridSubdivisions = EditorGUILayout.IntSlider("Subdivisions", settings.gridSubdivisions, 1, 16);
            }

            settings.colorBySubmesh = EditorGUILayout.Toggle("Color by Submesh", settings.colorBySubmesh);

            if (EditorGUI.EndChangeCheck()) MarkDirty();
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        #endregion

        #region Controls: Advanced Visualization

        void DrawAdvancedVisualization()
        {
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced Visualization", true);
            if (!_showAdvanced) return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            // Island coloring
            settings.colorByIsland = EditorGUILayout.Toggle("Color by Island", settings.colorByIsland);

            // Texel density
            settings.showTexelDensity = EditorGUILayout.Toggle("Texel Density Heatmap", settings.showTexelDensity);
            if (settings.showTexelDensity)
            {
                settings.texelDensityMin = EditorGUILayout.FloatField("Density Min", settings.texelDensityMin);
                settings.texelDensityMax = EditorGUILayout.FloatField("Density Max", settings.texelDensityMax);
            }

            // Seams
            settings.showSeams = EditorGUILayout.Toggle("Show Seams", settings.showSeams);
            if (settings.showSeams)
                settings.seamColor = EditorGUILayout.ColorField("Seam Color", settings.seamColor);

            // Out of bounds
            settings.highlightOutOfBounds = EditorGUILayout.Toggle("Highlight Out-of-Bounds", settings.highlightOutOfBounds);
            if (settings.highlightOutOfBounds)
                settings.outOfBoundsColor = EditorGUILayout.ColorField("OOB Color", settings.outOfBoundsColor);

            // Overlaps
            settings.highlightOverlaps = EditorGUILayout.Toggle("Highlight Overlaps", settings.highlightOverlaps);
            if (settings.highlightOverlaps)
                settings.overlapColor = EditorGUILayout.ColorField("Overlap Color", settings.overlapColor);

            // UDIM
            settings.showUDIM = EditorGUILayout.Toggle("Show UDIM Tiles", settings.showUDIM);

            // Vertex density
            settings.showVertexDensity = EditorGUILayout.Toggle("Vertex Density", settings.showVertexDensity);
            if (settings.showVertexDensity)
                settings.vertexDensityRadius = EditorGUILayout.Slider("VD Radius", settings.vertexDensityRadius, 0.005f, 0.1f);

            // Texture overlay
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Texture Overlay", EditorStyles.miniLabel);
            settings.overlayTexture = (Texture2D)EditorGUILayout.ObjectField(
                "Overlay Texture", settings.overlayTexture, typeof(Texture2D), false);
            if (settings.overlayTexture != null)
                settings.overlayOpacity = EditorGUILayout.Slider("Overlay Opacity", settings.overlayOpacity, 0f, 1f);

            if (EditorGUI.EndChangeCheck()) MarkDirty();
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        #endregion

        #region Controls: Export

        void DrawExportSettings()
        {
            _showExportSettings = EditorGUILayout.Foldout(_showExportSettings, "Export", true);
            if (!_showExportSettings) return;

            EditorGUI.indentLevel++;

            // Resolution
            int resIdx = System.Array.IndexOf(UVLayoutSettings.ResolutionOptions, settings.resolution);
            if (resIdx < 0) resIdx = 2;
            resIdx = EditorGUILayout.Popup("Resolution", resIdx,
                UVLayoutSettings.ResolutionOptions.Select(r => $"{r}x{r}").ToArray());
            int newRes = UVLayoutSettings.ResolutionOptions[resIdx];
            if (newRes != settings.resolution) { settings.resolution = newRes; MarkDirty(); }

            EditorGUI.BeginDisabledGroup(targetMesh == null || _preview == null);

            // PNG export
            if (GUILayout.Button("Export PNG"))
            {
                var exportTex = RenderForExport();
                if (exportTex != null)
                {
                    string path = UVLayoutExporter.ExportToPNG(exportTex,
                        $"{targetMesh.name}_uv{settings.uvChannel}");
                    DestroyImmediate(exportTex);
                    if (path != null) Debug.Log($"UV Layout exported to: {path}");
                }
            }

            // SVG export
            if (GUILayout.Button("Export SVG"))
            {
                var mesh = GetReadableMesh(targetMesh);
                if (mesh != null)
                {
                    string path = UVLayoutExporter.ExportToSVG(mesh, settings,
                        $"{targetMesh.name}_uv{settings.uvChannel}");
                    if (mesh != targetMesh) DestroyImmediate(mesh);
                    if (path != null) Debug.Log($"UV Layout SVG exported to: {path}");
                }
            }

            EditorGUI.EndDisabledGroup();

            // Batch export
            if (GUILayout.Button("Batch Export Selected (PNG)"))
            {
                var meshes = GetMeshesFromSelection();
                if (meshes.Length > 0)
                {
                    int count = UVLayoutExporter.BatchExport(meshes, settings);
                    Debug.Log($"Batch exported {count} UV layout(s).");
                }
                else
                    Debug.LogWarning("No meshes found in selection.");
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        Texture2D RenderForExport()
        {
            var mesh = GetReadableMesh(targetMesh);
            if (mesh == null) return null;

            var tex = UVLayoutRenderer.Render(new UVLayoutRenderer.RenderContext
            {
                Mesh = mesh,
                Settings = settings,
                OverlappingTris = _overlappingTris,
                OutOfBoundsTris = _outOfBoundsTris,
                Islands = _islands,
                IslandColors = _islandColors,
                TexelDensities = _texelDensities,
                SeamEdges = _seamEdges,
                VertexDensityMap = _vertexDensityMap,
                VertexDensityMax = _vertexDensityMax
            });

            if (mesh != targetMesh) DestroyImmediate(mesh);
            return tex;
        }

        #endregion

        #region Controls: Scene View

        void DrawSceneViewSettings()
        {
            _showSceneView = EditorGUILayout.Foldout(_showSceneView, "Scene View Overlay", true);
            if (!_showSceneView) return;

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            settings.checkerPatternScene = EditorGUILayout.Toggle("Checker Pattern", settings.checkerPatternScene);
            if (settings.checkerPatternScene)
                settings.checkerScale = EditorGUILayout.IntSlider("Checker Scale", settings.checkerScale, 2, 32);
            settings.texelDensityScene = EditorGUILayout.Toggle("Texel Density", settings.texelDensityScene);
            if (EditorGUI.EndChangeCheck())
            {
                if (settings.checkerPatternScene || settings.texelDensityScene)
                {
                    if (_targetTransformCache == null)
                    {
                        var (_, transform) = GetMeshFromSelection();
                        _targetTransformCache = transform;
                    }
                    if (_targetTransformCache != null && targetMesh != null)
                        UVLayoutSceneOverlay.Enable(targetMesh, _targetTransformCache, settings);
                    else
                        Debug.LogWarning("Select a GameObject in the scene to use scene view overlays.");
                }
                else
                {
                    UVLayoutSceneOverlay.Disable();
                }
            }

            if (UVLayoutSceneOverlay.IsActive)
            {
                EditorGUILayout.HelpBox("Scene overlay active. Select a GameObject with the mesh in scene.",
                    MessageType.Info);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        #endregion

        #region Controls: Analysis

        void DrawAnalysisSection()
        {
            _showAnalysis = EditorGUILayout.Foldout(_showAnalysis, "Analysis", true);
            if (!_showAnalysis) return;

            EditorGUI.indentLevel++;

            if (targetMesh == null)
            {
                EditorGUILayout.HelpBox("No mesh selected.", MessageType.Info);
            }
            else if (_meshNotReadable)
            {
                EditorGUILayout.HelpBox(
                    $"Mesh '{targetMesh.name}' is not readable.\nEnable Read/Write in import settings.",
                    MessageType.Error);
            }
            else if (!_stats.HasUVs)
            {
                EditorGUILayout.HelpBox($"No UVs on channel {settings.uvChannel}.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Triangles", _stats.TriangleCount.ToString());
                EditorGUILayout.LabelField("UV Coverage", $"{_stats.CoveragePercent:F1}%");
                EditorGUILayout.LabelField("Islands", _stats.IslandCount.ToString());

                if (_overlappingTris != null)
                    EditorGUILayout.LabelField("Overlapping Tris", _overlappingTris.Count.ToString());
                if (_outOfBoundsTris != null)
                    EditorGUILayout.LabelField("Out-of-Bounds Tris", _outOfBoundsTris.Count.ToString());
                if (_seamEdges != null)
                    EditorGUILayout.LabelField("Seam Edges", _seamEdges.Count.ToString());
                if (_hoveredIsland >= 0)
                    EditorGUILayout.LabelField("Hovered Island", $"#{_hoveredIsland} ({_islands[_hoveredIsland].Count} tris)");

                EditorGUILayout.Space(2);

                // Copy stats to clipboard
                if (GUILayout.Button("Copy Stats to Clipboard"))
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"Mesh: {targetMesh.name}");
                    sb.AppendLine($"UV Channel: {settings.uvChannel}");
                    sb.AppendLine($"Triangles: {_stats.TriangleCount}");
                    sb.AppendLine($"UV Coverage: {_stats.CoveragePercent:F1}%");
                    sb.AppendLine($"Islands: {_stats.IslandCount}");
                    if (_overlappingTris != null) sb.AppendLine($"Overlapping Tris: {_overlappingTris.Count}");
                    if (_outOfBoundsTris != null) sb.AppendLine($"Out-of-Bounds Tris: {_outOfBoundsTris.Count}");
                    if (_seamEdges != null) sb.AppendLine($"Seam Edges: {_seamEdges.Count}");
                    GUIUtility.systemCopyBuffer = sb.ToString();
                    Debug.Log("UV stats copied to clipboard.");
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        #endregion

        #region Controls: Lightmap Validation

        void DrawLightmapValidation()
        {
            _showLightmap = EditorGUILayout.Foldout(_showLightmap, "Lightmap UV Validation", true);
            if (!_showLightmap) return;

            EditorGUI.indentLevel++;

            EditorGUI.BeginDisabledGroup(targetMesh == null);
            if (GUILayout.Button("Validate Lightmap UVs (UV1)"))
            {
                var mesh = GetReadableMesh(targetMesh);
                if (mesh != null)
                {
                    _lightmapValidation = UVLayoutAnalyzer.ValidateLightmapUVs(mesh, settings.resolution);
                    _lightmapValidated = true;
                    if (mesh != targetMesh) DestroyImmediate(mesh);
                }
            }
            EditorGUI.EndDisabledGroup();

            if (_lightmapValidated)
            {
                var v = _lightmapValidation;
                if (!v.HasUV1)
                {
                    EditorGUILayout.HelpBox("No UV1 channel found.", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.LabelField("Coverage", $"{v.CoveragePercent:F1}%");
                    EditorGUILayout.LabelField("Overlapping Tris", v.OverlappingTriCount.ToString());
                    EditorGUILayout.LabelField("Out-of-Bounds Tris", v.OutOfBoundsTriCount.ToString());
                    EditorGUILayout.LabelField("Min Padding",
                        v.MinPaddingPixels < 999f ? $"{v.MinPaddingPixels:F1}px" : "N/A");

                    if (v.Issues.Count > 0)
                    {
                        EditorGUILayout.Space(2);
                        foreach (string issue in v.Issues)
                            EditorGUILayout.HelpBox(issue, MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Lightmap UVs look good!", MessageType.Info);
                    }
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        #endregion

        #region Controls: Snapshots

        void DrawSnapshotSection()
        {
            _showSnapshots = EditorGUILayout.Foldout(_showSnapshots, $"Snapshots ({_snapshots.Count})", true);
            if (!_showSnapshots) return;

            EditorGUI.indentLevel++;

            EditorGUI.BeginDisabledGroup(_preview == null);
            if (GUILayout.Button("Take Snapshot"))
            {
                if (_snapshots.Count >= MaxSnapshots)
                {
                    DestroyImmediate(_snapshots[0].tex);
                    _snapshots.RemoveAt(0);
                }

                var copy = new Texture2D(_preview.width, _preview.height, _preview.format, false);
                Graphics.CopyTexture(_preview, copy);
                string label = $"UV{settings.uvChannel} {settings.resolution}px";
                _snapshots.Add((copy, label));
            }
            EditorGUI.EndDisabledGroup();

            if (_snapshots.Count > 0 && GUILayout.Button("Clear All"))
                ClearSnapshots();

            EditorGUI.indentLevel--;
        }

        void DrawSnapshotStrip()
        {
            if (_snapshots.Count == 0) return;

            EditorGUILayout.BeginHorizontal(GUILayout.Height(80));
            _snapshotScroll = EditorGUILayout.BeginScrollView(_snapshotScroll,
                GUILayout.Height(84));
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < _snapshots.Count; i++)
            {
                var (tex, label) = _snapshots[i];
                EditorGUILayout.BeginVertical(GUILayout.Width(72));

                var rect = GUILayoutUtility.GetRect(68, 60);
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
                EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel,
                    GUILayout.Width(68));

                if (rect.Contains(Event.current.mousePosition) &&
                    Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    var menu = new GenericMenu();
                    int idx = i;
                    menu.AddItem(new GUIContent("Export PNG"), false, () =>
                    {
                        UVLayoutExporter.ExportToPNG(_snapshots[idx].tex, $"snapshot_{idx}");
                    });
                    menu.AddItem(new GUIContent("Remove"), false, () =>
                    {
                        DestroyImmediate(_snapshots[idx].tex);
                        _snapshots.RemoveAt(idx);
                        Repaint();
                    });
                    menu.ShowAsContext();
                    Event.current.Use();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Helpers

        static (Mesh mesh, Transform transform) GetMeshFromSelection()
        {
            var go = Selection.activeGameObject;
            if (go == null) return (null, null);

            var mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) return (mf.sharedMesh, go.transform);

            var smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null) return (smr.sharedMesh, go.transform);

            return (null, null);
        }

        static Mesh[] GetMeshesFromSelection()
        {
            var meshes = new HashSet<Mesh>();
            foreach (var go in Selection.gameObjects)
            {
                var mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) meshes.Add(mf.sharedMesh);

                var smr = go.GetComponent<SkinnedMeshRenderer>();
                if (smr != null && smr.sharedMesh != null) meshes.Add(smr.sharedMesh);
            }
            return meshes.ToArray();
        }

        #endregion
    }
}
#endif

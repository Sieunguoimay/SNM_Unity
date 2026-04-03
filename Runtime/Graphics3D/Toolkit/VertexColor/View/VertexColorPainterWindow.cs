#if UNITY_EDITOR
using Snm.Graphics3D.Rigging;
using Snm.Graphics3D.Toolkit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Graphics3D.VertexColor
{
    /// <summary>
    /// Editor window for vertex color painting. Provides mesh input, color picker,
    /// brush settings, and actions (fill, clear, export). Follows the BuildContent()
    /// pattern for embedding in the ToolkitHub.
    /// </summary>
    public class VertexColorPainterWindow : EditorWindow
    {
        private VertexColorDocument _doc;
        private VertexColorPaintMode _paintMode;
        private BrushSettings _brushSettings;
        private BrushSettingsPanel _brushPanel;
        private ColorField _colorField;
        private bool _isActive;

        private const string PrefKeyRadius = "VertexColorPainter.BrushRadius";
        private const string PrefKeyStrength = "VertexColorPainter.BrushStrength";
        private const string PrefKeyFalloff = "VertexColorPainter.BrushFalloff";

        [MenuItem("Tools/Snm/3D Toolkit/Modeling/Vertex Color Painter")]
        public static void ShowWindow()
        {
            var window = GetWindow<VertexColorPainterWindow>();
            window.titleContent = new GUIContent("Vertex Color Painter");
            window.minSize = new Vector2(280, 300);
        }

        private void OnEnable()
        {
            _brushSettings = LoadBrushPrefs();

            _doc = ScriptableObject.CreateInstance<VertexColorDocument>();
            _doc.hideFlags = HideFlags.DontSave;

            _paintMode = new VertexColorPaintMode(_brushSettings);

            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            SaveBrushPrefs(_brushSettings);

            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndoRedo;

            Deactivate();

            if (_doc != null)
            {
                DestroyImmediate(_doc);
                _doc = null;
            }
        }

        private void OnUndoRedo()
        {
            if (_isActive && _paintMode != null && _doc.sourceMesh != null)
            {
                // Force overlay rebuild after undo
                _paintMode.OnExit();
                _paintMode.OnEnter(_doc);
            }
            Repaint();
        }

        public void CreateGUI()
        {
            rootVisualElement.Add(BuildContent());
        }

        internal VisualElement BuildContent()
        {
            var root = new VisualElement { style = { flexGrow = 1f } };

            // === Mesh input ===
            var inputSection = new VisualElement { style = { paddingLeft = 4, paddingRight = 4, paddingTop = 4 } };

            var meshField = new ObjectField("Mesh") { objectType = typeof(Mesh) };
            meshField.RegisterValueChangedCallback(evt =>
            {
                var mesh = evt.newValue as Mesh;
                bool wasActive = _isActive;
                if (wasActive) Deactivate();

                _doc.Record("Set Mesh");
                _doc.sourceMesh = mesh;
                if (mesh != null)
                    _doc.EnsureVertexColors();

                if (wasActive && mesh != null) Activate();
            });
            inputSection.Add(meshField);

            // Mesh status indicator (IMGUI-based)
            inputSection.Add(new IMGUIContainer(() =>
            {
                if (_doc.sourceMesh != null)
                    ToolkitGUI.MeshStatus(_doc.sourceMesh);
            }));

            root.Add(inputSection);

            // === Separator ===
            root.Add(MakeSeparator());

            // === Color picker ===
            var colorSection = new VisualElement
            {
                style = { paddingLeft = 4, paddingRight = 4, paddingTop = 4, paddingBottom = 4 }
            };

            _colorField = new ColorField("Brush Color") { value = _doc.brushColor, showAlpha = true };
            _colorField.RegisterValueChangedCallback(evt =>
            {
                _doc.brushColor = evt.newValue;
            });
            colorSection.Add(_colorField);
            root.Add(colorSection);

            // === Separator ===
            root.Add(MakeSeparator());

            // === Brush settings ===
            _brushPanel = new BrushSettingsPanel();
            _brushPanel.Bind(_brushSettings);
            root.Add(_brushPanel);

            // === Separator ===
            root.Add(MakeSeparator());

            // === Activate / Deactivate toggle ===
            var toggleSection = new VisualElement
            {
                style = { paddingLeft = 4, paddingRight = 4, paddingTop = 4, paddingBottom = 4 }
            };

            var activateBtn = new Button { text = "Start Painting", style = { height = 28 } };
            activateBtn.clicked += () =>
            {
                if (_isActive)
                {
                    Deactivate();
                    activateBtn.text = "Start Painting";
                    activateBtn.style.backgroundColor = StyleKeyword.Null;
                }
                else
                {
                    if (_doc.sourceMesh == null)
                    {
                        EditorUtility.DisplayDialog("Vertex Color Painter", "Assign a mesh first.", "OK");
                        return;
                    }
                    Activate();
                    activateBtn.text = "Stop Painting";
                    activateBtn.style.backgroundColor = new Color(0.2f, 0.5f, 0.2f, 1f);
                }
            };
            toggleSection.Add(activateBtn);
            root.Add(toggleSection);

            // === Separator ===
            root.Add(MakeSeparator());

            // === Actions ===
            var actionsSection = new VisualElement
            {
                style = { paddingLeft = 4, paddingRight = 4, paddingTop = 4, paddingBottom = 4 }
            };

            var actionLabel = new Label("Actions") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } };
            actionsSection.Add(actionLabel);

            var actionRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var fillBtn = new Button(() =>
            {
                if (_doc.vertexColors == null) return;
                _doc.FillAll(_doc.brushColor);
                if (_paintMode != null && _isActive)
                {
                    _paintMode.OnExit();
                    _paintMode.OnEnter(_doc);
                }
                SceneView.RepaintAll();
            })
            { text = "Fill All", style = { flexGrow = 1 } };

            var clearBtn = new Button(() =>
            {
                if (_doc.vertexColors == null) return;
                _doc.ClearAll();
                if (_paintMode != null && _isActive)
                {
                    _paintMode.OnExit();
                    _paintMode.OnEnter(_doc);
                }
                SceneView.RepaintAll();
            })
            { text = "Clear (White)", style = { flexGrow = 1 } };

            actionRow.Add(fillBtn);
            actionRow.Add(clearBtn);
            actionsSection.Add(actionRow);
            root.Add(actionsSection);

            // === Separator ===
            root.Add(MakeSeparator());

            // === Export ===
            var exportSection = new VisualElement
            {
                style = { paddingLeft = 4, paddingRight = 4, paddingTop = 4, paddingBottom = 4, flexShrink = 0 }
            };

            var exportLabel = new Label("Export") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } };
            exportSection.Add(exportLabel);

            var exportBtn = new Button(ExportColorsToMesh)
            {
                text = "Bake Colors into Mesh",
                style = { height = 26 }
            };
            exportSection.Add(exportBtn);

            var exportNewBtn = new Button(ExportToNewMesh)
            {
                text = "Export as New Mesh Asset",
                style = { height = 26, marginTop = 2 }
            };
            exportSection.Add(exportNewBtn);

            root.Add(exportSection);

            return root;
        }

        private void Activate()
        {
            _doc.EnsureVertexColors();
            _paintMode.OnEnter(_doc);
            _isActive = true;
            SceneView.RepaintAll();
        }

        private void Deactivate()
        {
            if (_isActive)
            {
                _paintMode.OnExit();
                _isActive = false;
                SceneView.RepaintAll();
            }
        }

        private void OnSceneGUI(SceneView view)
        {
            if (!_isActive || _doc == null || _doc.sourceMesh == null) return;

            // Handle keyboard shortcuts
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (_paintMode.OnKeyDown(e.keyCode))
                {
                    _brushPanel?.RefreshFromBrush();
                    e.Use();
                }
            }

            _paintMode.OnSceneGUI(view);
        }

        private void ExportColorsToMesh()
        {
            if (_doc.sourceMesh == null || _doc.vertexColors == null)
            {
                EditorUtility.DisplayDialog("Export", "No mesh or colors to export.", "OK");
                return;
            }

            // Can only bake into an existing asset — not an imported sub-asset
            var location = ToolkitGUI.GetMeshLocation(_doc.sourceMesh);
            if (location == MeshLocation.SubAsset)
            {
                EditorUtility.DisplayDialog("Export",
                    "Cannot bake into an imported mesh. Use 'Export as New Mesh Asset' instead.", "OK");
                return;
            }

            Undo.RecordObject(_doc.sourceMesh, "Bake Vertex Colors");
            _doc.sourceMesh.colors = _doc.vertexColors;
            EditorUtility.SetDirty(_doc.sourceMesh);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(_doc.sourceMesh);
        }

        private void ExportToNewMesh()
        {
            if (_doc.sourceMesh == null || _doc.vertexColors == null)
            {
                EditorUtility.DisplayDialog("Export", "No mesh or colors to export.", "OK");
                return;
            }

            var copy = Object.Instantiate(_doc.sourceMesh);
            copy.colors = _doc.vertexColors;

            var saved = ToolkitGUI.SaveMeshAsset(copy, _doc.sourceMesh.name + "_colored");
            if (saved == null)
                Object.DestroyImmediate(copy);
        }

        private static VisualElement MakeSeparator()
        {
            return new VisualElement
            {
                style = { height = 1, backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f) }
            };
        }

        private static BrushSettings LoadBrushPrefs()
        {
            var s = new BrushSettings();
            if (EditorPrefs.HasKey(PrefKeyRadius))
                s.radius = EditorPrefs.GetFloat(PrefKeyRadius, 0.1f);
            if (EditorPrefs.HasKey(PrefKeyStrength))
                s.strength = EditorPrefs.GetFloat(PrefKeyStrength, 0.5f);
            if (EditorPrefs.HasKey(PrefKeyFalloff))
                s.falloff = EditorPrefs.GetFloat(PrefKeyFalloff, 0.5f);
            return s;
        }

        private static void SaveBrushPrefs(BrushSettings s)
        {
            if (s == null) return;
            EditorPrefs.SetFloat(PrefKeyRadius, s.radius);
            EditorPrefs.SetFloat(PrefKeyStrength, s.strength);
            EditorPrefs.SetFloat(PrefKeyFalloff, s.falloff);
        }
    }
}
#endif

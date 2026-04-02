#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Snm.Graphics3D.Modeling;
using Snm.Graphics3D.Inspection;
using Snm.Graphics3D.UVLayout;
using Snm.Graphics3D.Animation;
using Snm.Graphics3D.Rigging;

namespace Snm.Graphics3D.Toolkit
{
    public class ToolkitHubWindow : EditorWindow
    {
        enum PanelKind { Imgui, UIElements }

        struct ToolDef
        {
            public string Name;
            public string Category;
            public Type WindowType;
            public PanelKind Kind;
        }

        static readonly string[] Categories = { "Modeling", "Inspect", "UV", "Animation", "Rigging" };

        static readonly ToolDef[] Tools =
        {
            new() { Name = "Primitives",  Category = "Modeling",   WindowType = typeof(MeshPrimitivesWindow),        Kind = PanelKind.Imgui },
            new() { Name = "Mesh Editor", Category = "Modeling",   WindowType = typeof(MeshEditorWindow),            Kind = PanelKind.Imgui },
            new() { Name = "Boolean",     Category = "Modeling",   WindowType = typeof(MeshBooleanWindow),           Kind = PanelKind.Imgui },
            new() { Name = "Decimator",   Category = "Modeling",   WindowType = typeof(MeshDecimatorWindow),         Kind = PanelKind.Imgui },
            new() { Name = "Combiner",    Category = "Modeling",   WindowType = typeof(MeshCombinerWindow),          Kind = PanelKind.Imgui },
            new() { Name = "Normals",     Category = "Modeling",   WindowType = typeof(NormalsEditorWindow),         Kind = PanelKind.Imgui },
            new() { Name = "Pivot",       Category = "Modeling",   WindowType = typeof(PivotEditorWindow),           Kind = PanelKind.Imgui },
            new() { Name = "Inspector",   Category = "Inspect",    WindowType = typeof(MeshInspectorWindow),         Kind = PanelKind.Imgui },
            new() { Name = "Exporter",    Category = "Inspect",    WindowType = typeof(MeshExporterWindow),          Kind = PanelKind.Imgui },
            new() { Name = "UV Layout",   Category = "UV",         WindowType = typeof(UVLayoutWindow),              Kind = PanelKind.Imgui },
            new() { Name = "Anim Baker",  Category = "Animation",  WindowType = typeof(AnimationBakerWindow),        Kind = PanelKind.UIElements },
            new() { Name = "Anim Preview",Category = "Animation",  WindowType = typeof(BakedAnimationPreviewWindow), Kind = PanelKind.UIElements },
            new() { Name = "Bone Editor", Category = "Rigging",    WindowType = typeof(BoneToolV2Window),            Kind = PanelKind.UIElements },
        };

        // Tab colors
        static readonly Color CatActiveBg   = new(0.22f, 0.44f, 0.82f, 0.55f);
        static readonly Color CatInactiveBg = new(0.16f, 0.16f, 0.16f, 1f);
        static readonly Color ToolActiveBg  = new(0.28f, 0.50f, 0.85f, 0.40f);
        static readonly Color ToolBarBg     = new(0.19f, 0.19f, 0.19f, 1f);
        static readonly Color DividerColor  = new(0.12f, 0.12f, 0.12f, 1f);

        [SerializeField] int categoryIndex;
        [SerializeField] int toolIndex;

        EditorWindow[] _panelInstances;
        VisualElement _categoryBar;
        VisualElement _toolBar;
        VisualElement _contentContainer;
        readonly List<Button> _categoryButtons = new();
        readonly List<Button> _toolButtons = new();
        int _builtCategory = -1;
        int _builtTool = -1;

        [MenuItem("Tools/Snm/3D Toolkit/Hub", priority = -100)]
        public static void Open()
        {
            var w = GetWindow<ToolkitHubWindow>("3D Toolkit");
            w.minSize = new Vector2(380, 350);
        }

        void OnEnable()
        {
            _panelInstances = new EditorWindow[Tools.Length];
            Selection.selectionChanged += OnRepaint;
            Undo.undoRedoPerformed += OnRepaint;
        }

        void OnDisable()
        {
            Selection.selectionChanged -= OnRepaint;
            Undo.undoRedoPerformed -= OnRepaint;
            DestroyPanelInstances();
        }

        void OnRepaint() => Repaint();

        void DestroyPanelInstances()
        {
            if (_panelInstances == null) return;
            for (int i = 0; i < _panelInstances.Length; i++)
            {
                if (_panelInstances[i] != null)
                {
                    DestroyImmediate(_panelInstances[i]);
                    _panelInstances[i] = null;
                }
            }
        }

        EditorWindow GetOrCreatePanel(int index)
        {
            if (_panelInstances == null || index < 0 || index >= _panelInstances.Length) return null;
            if (_panelInstances[index] == null)
                _panelInstances[index] = (EditorWindow)CreateInstance(Tools[index].WindowType);
            return _panelInstances[index];
        }

        void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexGrow = 1;

            // Category bar
            _categoryBar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    backgroundColor = CatInactiveBg,
                    paddingLeft = 1, paddingRight = 1,
                    paddingTop = 1, paddingBottom = 0,
                }
            };
            BuildCategoryButtons();
            root.Add(_categoryBar);

            // Divider
            root.Add(MakeDivider());

            // Tool bar
            _toolBar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    backgroundColor = ToolBarBg,
                    paddingLeft = 2, paddingRight = 2,
                    paddingTop = 1, paddingBottom = 1,
                }
            };
            root.Add(_toolBar);

            // Divider
            root.Add(MakeDivider());

            // Content
            _contentContainer = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    paddingLeft = 2, paddingRight = 2,
                    paddingTop = 2,
                }
            };
            root.Add(_contentContainer);

            BuildToolButtons();
            ShowPanel();
        }

        static VisualElement MakeDivider()
        {
            return new VisualElement
            {
                style = { height = 1, backgroundColor = DividerColor }
            };
        }

        void BuildCategoryButtons()
        {
            _categoryBar.Clear();
            _categoryButtons.Clear();

            for (int i = 0; i < Categories.Length; i++)
            {
                int idx = i;
                var btn = MakeTabButton(Categories[i], () => SelectCategory(idx));
                _categoryButtons.Add(btn);
                _categoryBar.Add(btn);
            }

            UpdateCategoryHighlight();
        }

        void BuildToolButtons()
        {
            if (_builtCategory == categoryIndex) return;
            _builtCategory = categoryIndex;

            _toolBar.Clear();
            _toolButtons.Clear();

            string cat = Categories[categoryIndex];
            int localIdx = 0;

            for (int i = 0; i < Tools.Length; i++)
            {
                if (Tools[i].Category != cat) continue;

                int capturedLocal = localIdx;
                var btn = MakeTabButton(Tools[i].Name, () => SelectTool(capturedLocal));
                btn.style.fontSize = 11;
                _toolButtons.Add(btn);
                _toolBar.Add(btn);
                localIdx++;
            }

            if (toolIndex >= _toolButtons.Count) toolIndex = 0;
            UpdateToolHighlight();
        }

        static Button MakeTabButton(string text, Action onClick)
        {
            var btn = new Button(onClick)
            {
                text = text,
                style =
                {
                    marginLeft = 1, marginRight = 1,
                    marginTop = 0, marginBottom = 0,
                    paddingLeft = 8, paddingRight = 8,
                    paddingTop = 3, paddingBottom = 3,
                    borderTopLeftRadius = 3, borderTopRightRadius = 3,
                    borderBottomLeftRadius = 0, borderBottomRightRadius = 0,
                    borderLeftWidth = 0, borderRightWidth = 0,
                    borderTopWidth = 0, borderBottomWidth = 0,
                    fontSize = 11,
                }
            };
            return btn;
        }

        void SelectCategory(int idx)
        {
            if (categoryIndex == idx) return;
            categoryIndex = idx;
            toolIndex = 0;
            _builtCategory = -1;
            _builtTool = -1;
            BuildToolButtons();
            UpdateCategoryHighlight();
            ShowPanel();
        }

        void SelectTool(int localIdx)
        {
            toolIndex = localIdx;
            _builtTool = -1;
            UpdateToolHighlight();
            ShowPanel();
        }

        void UpdateCategoryHighlight()
        {
            for (int i = 0; i < _categoryButtons.Count; i++)
            {
                bool active = i == categoryIndex;
                var btn = _categoryButtons[i];
                btn.style.backgroundColor = active ? CatActiveBg : CatInactiveBg;
                btn.style.color = active ? Color.white : new Color(0.65f, 0.65f, 0.65f);
                btn.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        void UpdateToolHighlight()
        {
            for (int i = 0; i < _toolButtons.Count; i++)
            {
                bool active = i == toolIndex;
                var btn = _toolButtons[i];
                btn.style.backgroundColor = active ? ToolActiveBg : ToolBarBg;
                btn.style.color = active ? Color.white : new Color(0.7f, 0.7f, 0.7f);
                btn.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        void ShowPanel()
        {
            int globalIdx = GetGlobalIndex();
            if (globalIdx < 0 || globalIdx == _builtTool) return;
            _builtTool = globalIdx;

            _contentContainer.Clear();

            var panel = GetOrCreatePanel(globalIdx);
            if (panel == null) return;

            if (Tools[globalIdx].Kind == PanelKind.Imgui)
            {
                var imgui = new IMGUIContainer(() => CallDrawContent(panel))
                {
                    style = { flexGrow = 1 }
                };
                _contentContainer.Add(imgui);
            }
            else
            {
                var content = CallBuildContent(panel);
                if (content != null)
                {
                    content.style.flexGrow = 1;
                    _contentContainer.Add(content);
                }
            }
        }

        int GetGlobalIndex()
        {
            string cat = Categories[categoryIndex];
            int localIdx = 0;
            for (int i = 0; i < Tools.Length; i++)
            {
                if (Tools[i].Category != cat) continue;
                if (localIdx == toolIndex) return i;
                localIdx++;
            }
            return -1;
        }

        static void CallDrawContent(EditorWindow window)
        {
            switch (window)
            {
                case MeshPrimitivesWindow w: w.DrawContent(); break;
                case MeshEditorWindow w:     w.DrawContent(); break;
                case MeshBooleanWindow w:    w.DrawContent(); break;
                case MeshDecimatorWindow w:  w.DrawContent(); break;
                case MeshCombinerWindow w:   w.DrawContent(); break;
                case NormalsEditorWindow w:  w.DrawContent(); break;
                case PivotEditorWindow w:    w.DrawContent(); break;
                case MeshInspectorWindow w:  w.DrawContent(); break;
                case MeshExporterWindow w:   w.DrawContent(); break;
                case UVLayoutWindow w:       w.DrawContent(); break;
            }
        }

        static VisualElement CallBuildContent(EditorWindow window)
        {
            return window switch
            {
                AnimationBakerWindow w => w.BuildContent(),
                BakedAnimationPreviewWindow w => w.BuildContent(),
                BoneToolV2Window w => w.BuildContent(),
                _ => null
            };
        }
    }
}
#endif

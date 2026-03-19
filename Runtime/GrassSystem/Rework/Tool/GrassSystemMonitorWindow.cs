#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Runtime.GrassSystem
{
    public class GrassSystemMonitorWindow : EditorWindow
    {
        private GrassSystemHandle _handle;
        private GrassFieldEntrypointMB _attachedMB;

        private VisualElement _contentContainer;
        private VisualElement _disturberContainer;

        [MenuItem("Tools/Snm/Grass System")]
        private static void Open()
        {
            var window = GetWindow<GrassSystemMonitorWindow>();
            window.titleContent = new GUIContent("Grass Monitor");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Detach();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            Detach();
        }

        private void CreateGUI()
        {
            var scrollView = new ScrollView();
            rootVisualElement.Add(scrollView);

            // Toolbar
            var toolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 8,
                    marginTop = 4,
                    marginLeft = 4,
                    marginRight = 4
                }
            };

            var attachButton = new Button(AttachToScene) { text = "Attach to Scene" };
            var detachButton = new Button(Detach) { text = "Detach" };

            toolbar.Add(attachButton);
            toolbar.Add(detachButton);
            scrollView.Add(toolbar);

            // Content
            _contentContainer = new VisualElement { name = "content-container" };
            scrollView.Add(_contentContainer);
        }

        private void AttachToScene()
        {
            Detach();

            _attachedMB = FindFirstObjectByType<GrassFieldEntrypointMB>();
            if (_attachedMB == null)
            {
                Debug.LogWarning("[GrassMonitor] No GrassFieldEntrypointMB found in scene.");
                return;
            }

            _handle = _attachedMB.SystemHandle;
            if (_handle == null)
            {
                Debug.LogWarning("[GrassMonitor] GrassFieldEntrypointMB found but has no active handle. Is it running?");
                return;
            }

            BuildMonitorUI();
        }

        private void Detach()
        {
            _contentContainer?.Clear();
            _disturberContainer = null;
            _handle = null;
            _attachedMB = null;
        }

        private void BuildMonitorUI()
        {
            _contentContainer.Clear();

            // Source label
            _contentContainer.Add(new Label($"Attached to: {_attachedMB.gameObject.name}")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 8,
                    marginLeft = 4
                }
            });

            BuildSystemInfoSection();
            BuildWindSection();
            BuildTrampleSection();
            BuildDisturberSection();
        }

        private void BuildSystemInfoSection()
        {
            var foldout = new Foldout { text = "System Info", value = true };

            var config = _handle.Config;
            var field = _handle.GrassField;
            var canvas = _handle.Canvas;

            // Grid info
            if (field != null)
            {
                foldout.Add(MakeInfoLabel($"Grid Dimension: {field.Dimension.x} x {field.Dimension.y}"));
                foldout.Add(MakeInfoLabel($"Cell Spacing: {field.Spacing.x:F2} x {field.Spacing.y:F2}"));
            }

            foldout.Add(MakeInfoLabel($"Instance Count: {_handle.InstanceCount}"));

            // Canvas info
            if (canvas != null)
            {
                foldout.Add(MakeInfoLabel($"Canvas Position: ({canvas.Position.x:F1}, {canvas.Position.y:F1}, {canvas.Position.z:F1})"));
                foldout.Add(MakeInfoLabel($"Canvas Size: {canvas.Size.x:F1} x {canvas.Size.y:F1}"));
            }

            // Mesh / Material
            if (config != null)
            {
                if (config.grassMesh != null)
                    foldout.Add(MakeInfoLabel($"Mesh: {config.grassMesh.name}  ({config.grassMesh.vertexCount} verts, {config.grassMesh.triangles.Length / 3} tris)"));
                if (config.grassMaterial != null)
                    foldout.Add(MakeInfoLabel($"Material: {config.grassMaterial.name}  (Shader: {config.grassMaterial.shader.name})"));
            }

            _contentContainer.Add(foldout);
        }

        private void BuildWindSection()
        {
            var config = _handle.Config?.windConfig;
            if (config == null) return;

            var foldout = new Foldout { text = "Wind", value = true };

            foldout.Add(MakeInfoLabel($"Strength: {config.strength:F2}  |  Scroll Speed: {config.scrollSpeed:F3}  |  Map Scale: ({config.mapScale.x:F1}, {config.mapScale.y:F1})"));

            // Wind DUDV preview
            if (config.dudvMap != null)
            {
                foldout.Add(MakeTextureLabel($"DUDV Map: {config.dudvMap.name}  ({config.dudvMap.width}x{config.dudvMap.height})"));
                foldout.Add(MakeTexturePreview(config.dudvMap, refreshing: false));
            }
            else
            {
                foldout.Add(MakeInfoLabel("DUDV Map: (none)"));
            }

            _contentContainer.Add(foldout);
        }

        private void BuildTrampleSection()
        {
            var trampleConfig = _handle.Config?.trampleSystemConfig;
            var trampleTex = _handle.TrampleTexture;

            var foldout = new Foldout { text = "Trample System", value = true };

            if (trampleConfig != null)
            {
                var enabledText = trampleConfig.enabled ? "Enabled" : "Disabled";
                foldout.Add(MakeInfoLabel($"Status: {enabledText}  |  Fade Speed: {trampleConfig.fadeSpeed:F2}  |  Min Offset: {trampleConfig.brushMinOffset:F3}"));
            }

            // Trample texture preview
            if (trampleTex != null)
            {
                foldout.Add(MakeTextureLabel($"Trample Map: {trampleTex.width}x{trampleTex.height}  ({trampleTex.graphicsFormat})"));
                foldout.Add(MakeTexturePreview(trampleTex, refreshing: true));
            }

            _contentContainer.Add(foldout);
        }

        private void BuildDisturberSection()
        {
            var tracker = _handle.DisturberTracker;

            var foldout = new Foldout { text = "Disturbers", value = true };

            if (tracker == null)
            {
                foldout.Add(new Label("Disturber tracking not available.")
                {
                    style = { color = new Color(0.6f, 0.6f, 0.6f) }
                });
                _contentContainer.Add(foldout);
                return;
            }

            _disturberContainer = new VisualElement();
            foldout.Add(_disturberContainer);

            _disturberContainer.schedule.Execute(_ => RefreshDisturberList()).Every(100);

            _contentContainer.Add(foldout);
        }

        private void RefreshDisturberList()
        {
            if (_disturberContainer == null || _handle?.DisturberTracker == null) return;

            var tracker = _handle.DisturberTracker;
            _disturberContainer.Clear();

            _disturberContainer.Add(new Label($"External: {tracker.ExternalCount}  |  Active: {tracker.ActiveCount}")
            {
                style = { marginBottom = 4, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 }
            });

            var snapshots = tracker.GetSnapshots();

            if (snapshots.Count == 0)
            {
                _disturberContainer.Add(new Label("No disturbers tracked.")
                {
                    style = { color = new Color(0.5f, 0.5f, 0.5f), unityFontStyleAndWeight = FontStyle.Italic }
                });
                return;
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                var s = snapshots[i];
                var inCanvasText = s.IsInCanvas ? "IN CANVAS" : "outside";
                var text = $"  [{i}]  pos({s.Position.x:F1}, {s.Position.y:F1}, {s.Position.z:F1})  " +
                           $"dir({s.Direction.x:F2}, {s.Direction.z:F2})  r: {s.Radius:F2}  {inCanvasText}";

                _disturberContainer.Add(new Label(text)
                {
                    style =
                    {
                        fontSize = 11,
                        color = s.IsInCanvas ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.6f, 0.6f, 0.6f),
                        unityFontStyleAndWeight = FontStyle.Normal
                    }
                });
            }
        }

        private static Label MakeInfoLabel(string text)
        {
            return new Label(text)
            {
                style = { fontSize = 11, color = new Color(0.8f, 0.8f, 0.8f), marginBottom = 2 }
            };
        }

        private static Label MakeTextureLabel(string text)
        {
            return new Label(text)
            {
                style = { fontSize = 11, color = new Color(0.7f, 0.7f, 0.7f), marginTop = 4 }
            };
        }

        private static VisualElement MakeTexturePreview(Texture texture, bool refreshing)
        {
            var img = new Image
            {
                image = texture,
                style =
                {
                    flexGrow = 0,
                    width = 256,
                    height = 256,
                    marginTop = 4,
                    marginBottom = 8
                }
            };

            if (refreshing)
            {
                img.schedule.Execute(_ => img.MarkDirtyRepaint()).Every(100);
            }

            return img;
        }
    }
}
#endif

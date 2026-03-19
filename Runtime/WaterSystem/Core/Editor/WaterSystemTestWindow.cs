#if UNITY_EDITOR
using System.Linq;
using Snm.WaterSystem.Wave;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.WaterSystem
{
    public class WaterSystemTestWindow : EditorWindow
    {
        [SerializeField] private WaterConfig config = new();

        [Header("Camera")]
        [SerializeField] private Camera sourceCamera;

        private WaterSystemHandle _handle;
        private WaterSystemMB _attachedMB;
        private bool _ownsHandle;

        private VisualElement _contentContainer;
        private Editor _settingsEditor;
        private WaveSimulationView _waveSimulationView;
        private VisualElement _disturberContainer;

        [MenuItem("Tools/Snm/Water System")]
        private static void Open()
        {
            var window = GetWindow<WaterSystemTestWindow>();
            window.titleContent = new GUIContent("Water Monitor");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            AutoAssignConfigReferences(config);
            sourceCamera = Camera.main;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Detach();

            if (_settingsEditor != null)
            {
                DestroyImmediate(_settingsEditor);
                _settingsEditor = null;
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            Detach();
        }

        private void CreateGUI()
        {
            var scrollView = new ScrollView();
            rootVisualElement.Add(scrollView);

            // ── Toolbar ──
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
            var runButton = new Button(RunStandalone) { text = "Run Standalone" };
            var stopButton = new Button(Detach) { text = "Stop" };

            toolbar.Add(attachButton);
            toolbar.Add(runButton);
            toolbar.Add(stopButton);
            scrollView.Add(toolbar);

            // ── Settings (for standalone mode) ──
            _settingsEditor = Editor.CreateEditor(this);
            var inspector = new IMGUIContainer(() => { _settingsEditor.OnInspectorGUI(); })
            {
                style = { flexShrink = 1, flexGrow = 0 }
            };
            scrollView.Add(inspector);

            // ── Content ──
            _contentContainer = new VisualElement { name = "content-container" };
            scrollView.Add(_contentContainer);
        }

        // ── Attach to scene WaterSystemMB ──
        private void AttachToScene()
        {
            Detach();

            _attachedMB = FindFirstObjectByType<WaterSystemMB>();
            if (_attachedMB == null)
            {
                Debug.LogWarning("[WaterMonitor] No WaterSystemMB found in scene.");
                return;
            }

            var handle = _attachedMB.Handle;
            if (handle == null)
            {
                Debug.LogWarning("[WaterMonitor] WaterSystemMB found but has no active handle. Is it running?");
                return;
            }

            _handle = handle;
            _ownsHandle = false;

            BuildMonitorUI();
        }

        // ── Run standalone test ──
        private void RunStandalone()
        {
            Detach();

            var resolvedCamera = sourceCamera;
            if (resolvedCamera == null)
            {
                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView == null)
                {
                    Debug.LogError("[WaterMonitor] No camera assigned and no active SceneView found.");
                    return;
                }
                resolvedCamera = sceneView.camera;
            }

            _handle = WaterSystemFactory.Create(config, resolvedCamera);
            _ownsHandle = true;
            _attachedMB = null;

            BuildMonitorUI();
        }

        private void Detach()
        {
            _waveSimulationView?.Detach();
            _contentContainer?.Clear();
            _disturberContainer = null;

            if (_ownsHandle)
                _handle?.Dispose();

            _handle = null;
            _attachedMB = null;
            _ownsHandle = false;
        }

        // ── Build UI ──
        private void BuildMonitorUI()
        {
            _contentContainer.Clear();

            // Source label
            string source = _attachedMB != null
                ? $"Attached to: {_attachedMB.gameObject.name}"
                : "Standalone test system";
            _contentContainer.Add(new Label(source)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 8,
                    marginLeft = 4
                }
            });

            // Reflection texture
            AddTexturePreview("Reflection Output", _handle.ReflectionTexture);

            // Wave simulation
            if (_handle.WaveSimulation != null)
            {
                _waveSimulationView = new WaveSimulationView();
                _waveSimulationView.Attach(_handle.WaveSimulation);

                var foldout = new Foldout { text = "Wave Simulation", value = true };
                foldout.Add(_waveSimulationView.Root);
                _contentContainer.Add(foldout);
            }

            // Disturber monitor
            BuildDisturberUI();
        }

        private void BuildDisturberUI()
        {
            var tracker = _handle.DisturberTracker;

            var foldout = new Foldout { text = "Disturbers", value = true };

            if (tracker == null)
            {
                foldout.Add(new Label("Disturber system not active.")
                {
                    style = { color = new Color(0.6f, 0.6f, 0.6f) }
                });
                _contentContainer.Add(foldout);
                return;
            }

            // Config summary
            var cfg = tracker.Config;
            var configLabel = new Label(
                $"Entry scale: {cfg.entryStrengthScale:F2}  |  Max entry: {cfg.entryMaxStrength:F2}  |  " +
                $"Wake: {cfg.wakeStrength:F3}  |  Speed: {cfg.wakeMinSpeed:F2}-{cfg.wakeMaxSpeed:F2}  |  " +
                $"Spacing: {cfg.wakeSpacingAtFullSpeed:F2}-{cfg.wakeSpacingAtMinSpeed:F2}  |  Proximity: {cfg.proximityTolerance:F2}")
            {
                style = { fontSize = 10, color = new Color(0.7f, 0.7f, 0.7f), marginBottom = 4 }
            };
            foldout.Add(configLabel);

            _disturberContainer = new VisualElement();
            foldout.Add(_disturberContainer);

            // Schedule periodic refresh
            _disturberContainer.schedule.Execute(_ => RefreshDisturberList()).Every(100);

            _contentContainer.Add(foldout);
        }

        private void RefreshDisturberList()
        {
            if (_disturberContainer == null || _handle?.DisturberTracker == null) return;

            var snapshot = _handle.DisturberTracker.Snapshot;

            _disturberContainer.Clear();

            if (snapshot.Count == 0)
            {
                _disturberContainer.Add(new Label("No disturbers tracked.")
                {
                    style = { color = new Color(0.5f, 0.5f, 0.5f), unityFontStyleAndWeight = FontStyle.Italic }
                });
                return;
            }

            _disturberContainer.Add(new Label($"Tracking {snapshot.Count} disturber(s):")
            {
                style = { marginBottom = 4, unityFontStyleAndWeight = FontStyle.Bold }
            });

            for (int i = 0; i < snapshot.Count; i++)
            {
                var info = snapshot[i];
                var speed = info.Velocity.magnitude;
                var statusIcon = info.IsInWater ? "~ " : "  ";
                var text = $"{statusIcon}[{i}]  pos({info.Position.x:F1}, {info.Position.y:F1}, {info.Position.z:F1})  " +
                           $"speed: {speed:F2}  contact: {info.ContactRadius:F2}  " +
                           $"{(info.IsInWater ? "IN WATER" : "above")}";

                _disturberContainer.Add(new Label(text)
                {
                    style =
                    {
                        fontSize = 11,
                        color = info.IsInWater ? new Color(0.4f, 0.8f, 1f) : new Color(0.6f, 0.6f, 0.6f),
                        unityFontStyleAndWeight = FontStyle.Normal
                    }
                });
            }
        }

        private void AddTexturePreview(string label, Texture texture)
        {
            if (texture == null) return;

            var outputLabel = new Label(label)
            {
                style = { unityTextAlign = TextAnchor.MiddleCenter, color = new Color(0.7f, 0.7f, 0.7f) }
            };
            _contentContainer.Add(outputLabel);

            var img = new Image
            {
                image = texture,
                style = { flexGrow = 1, minHeight = 200, marginTop = 4, marginBottom = 8 }
            };
            _contentContainer.Add(img);
            img.schedule.Execute(_ => img.MarkDirtyRepaint()).Every(50);
        }

        public static void AutoAssignConfigReferences(WaterConfig config)
        {
            if (config == null) return;
            config.surface.waterSurfaceShader = AssetDatabase.FindAssets("t:Shader WaterSurface").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Shader>).FirstOrDefault();
            config.caustics.causticsTexture = AssetDatabase.FindAssets("t:Texture2D caustics").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Texture2D>).FirstOrDefault();
            config.wave.simulationShader = AssetDatabase.FindAssets("t:Shader WaveSimulation").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Shader>).FirstOrDefault();
            config.wave.displayShader = AssetDatabase.FindAssets("t:Shader WaveDisplay").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Shader>).FirstOrDefault();
        }
    }
}
#endif

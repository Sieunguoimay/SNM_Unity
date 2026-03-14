#if UNITY_EDITOR
using System.Linq;
using Snm.Reactivity;
using Snm.Reactivity.Unity;
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
        private VisualElement _simulationContainer;
        private Editor _settingsEditor;
        private WaveSimulationView _waveSimulationView;

        [MenuItem("Tools/Snm/Water System")]
        private static void Open()
        {
            var window = GetWindow<WaterSystemTestWindow>();
            window.titleContent = new GUIContent("Water System");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            AutoAssignConfigReferences(config);
            sourceCamera = Camera.main;
        }

        private void OnDisable()
        {
            Stop();

            if (_settingsEditor != null)
            {
                DestroyImmediate(_settingsEditor);
                _settingsEditor = null;
            }
        }

        private void CreateGUI()
        {
            var toolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 8
                }
            };

            var signal_Run = new Signal<bool>(false);
            var runButton = new Button() { clickable = new(RunButtonClicked) };
            UIBindingUtil.AutoDispose(runButton, new(() => runButton.text = signal_Run.Value ? "\u25a0 Stop" : "\u25b6 Run"));

            void RunButtonClicked()
            {
                if (signal_Run.Value) Stop();
                else Run();
                signal_Run.Value = !signal_Run.Value;
            }

            toolbar.Add(runButton);

            _settingsEditor = Editor.CreateEditor(this);
            var inspector = new IMGUIContainer(() => { _settingsEditor.OnInspectorGUI(); })
            {
                style = { flexShrink = 1, flexGrow = 0 }
            };

            var scrollView = new ScrollView();
            scrollView.Add(inspector);
            scrollView.Add(toolbar);
            scrollView.Add(new VisualElement { name = "simulation-container" });
            rootVisualElement.Add(scrollView);
        }

        private void Run()
        {
            Stop();

            var resolvedCamera = sourceCamera;
            if (resolvedCamera == null)
            {
                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView == null)
                {
                    Debug.LogError("No camera assigned and no active SceneView found.");
                    return;
                }
                resolvedCamera = sceneView.camera;
            }

            _handle = WaterSystemInstaller.Install(config, resolvedCamera);

            _simulationContainer = rootVisualElement.Q<VisualElement>("simulation-container");
            _simulationContainer.Clear();

            // Texture previews
            AddTexturePreview("Reflection Output", _handle.ReflectionTexture);

            if (_handle.WaveSimulation != null)
            {
                _waveSimulationView = new WaveSimulationView();
                _waveSimulationView.Attach(_handle.WaveSimulation);

                var foldout_Wave = new Foldout() { text = "Wave Simulation" };
                foldout_Wave.Add(_waveSimulationView.Root);
                _simulationContainer.Add(foldout_Wave);
            }
        }

        private void AddTexturePreview(string label, Texture texture)
        {
            if (texture == null) return;

            var outputLabel = new Label(label)
            {
                style = { unityTextAlign = TextAnchor.MiddleCenter, color = new Color(0.7f, 0.7f, 0.7f) }
            };
            _simulationContainer.Add(outputLabel);

            var img = new Image
            {
                image = texture,
                style =
                {
                    flexGrow = 1,
                    minHeight = 200,
                    marginTop = 4,
                    marginBottom = 8,
                }
            };
            _simulationContainer.Add(img);
            img.schedule.Execute(_ => img.MarkDirtyRepaint()).Every(50);
        }

        private void Stop()
        {
            _waveSimulationView?.Detach();
            _simulationContainer?.Clear();
            _handle?.Dispose();
            _handle = null;
        }

        public static void AutoAssignConfigReferences(WaterConfig config)
        {
            if (config == null) return;
            config.surface.waterSurfaceShader = AssetDatabase.FindAssets($"t:Shader WaterSurface").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Shader>).FirstOrDefault();
            config.caustics.causticsTexture = AssetDatabase.FindAssets($"t:Texture2D caustics").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Texture2D>).FirstOrDefault();
            config.wave.simulationShader = AssetDatabase.FindAssets($"t:Shader WaveSimulation").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Shader>).FirstOrDefault();
            config.wave.displayShader = AssetDatabase.FindAssets($"t:Shader WaveDisplay").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Shader>).FirstOrDefault();
        }
    }
}
#endif
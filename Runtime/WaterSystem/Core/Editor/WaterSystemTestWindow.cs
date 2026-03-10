using Snm.Reactivity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.WaterSystem
{
    public class WaterSystemTestWindow : EditorWindow
    {
        [Header("Water Surface")]
        [SerializeField] private Shader waterSurfaceShader;
        [SerializeField] private Material waterSurfaceMaterial;
        [SerializeField] private Vector2 surfaceSize = new(10f, 10f);

        [Header("Camera")]
        [SerializeField] private Camera sourceCamera;

        [Header("Reflection")]
        [SerializeField] private bool reflectionEnabled = true;
        [SerializeField] private int reflectionTextureWidth = 256;

        [Header("Caustics")]
        [SerializeField] private bool causticsEnabled = true;
        [SerializeField] private Texture2D causticsTexture;
        [SerializeField, Range(0f, 5f)] private float causticsStrength = 1f;
        [SerializeField, Range(0.01f, 1f)] private float causticsScale = 0.1f;
        [SerializeField, Range(0f, 0.5f)] private float causticsSpeed = 0.05f;
        [SerializeField, Range(0f, 0.01f)] private float causticsSplit = 0.003f;

        [Header("Depth")]
        [SerializeField] private bool depthEnabled = true;
        [SerializeField] private Color shallowColor = Color.white;
        [SerializeField] private Color deepColor = Color.black;
        [SerializeField, Range(0f, 2f)] private float absorption = 0.4f;

        [Header("Wave")]
        [SerializeField] private bool waveEnabled;
        [SerializeField] private Shader waveSimulationShader;
        [SerializeField] private Shader waveDisplayShader;
        [SerializeField] private int waveTextureSize = 512;
        [SerializeField, Range(0.9f, 1f)] private float waveDamping = 0.99f;
        [SerializeField, Range(0.01f, 0.5f)] private float waveSpeed = 0.45f;
        [SerializeField, Range(0.1f, 10f)] private float waveSpreadSpeed = 1f;
        [SerializeField] private float waveHeightfieldStrength = 1f;

        private WaterSystemHandle _handle;
        private WaterConfig _config;
        private GameObject _contextGO;
        private Material _material;
        private bool _ownsMaterial;
        private Image _reflectionImage;
        private Image _waveImage;
        private VisualElement _simulationContainer;
        private Editor _settingsEditor;

        // Reactive signals for live-tweakable parameters
        private Signal<Texture2D> _causticsTextureSignal;
        private Signal<float> _causticsStrengthSignal;
        private Signal<float> _causticsScaleSignal;
        private Signal<float> _causticsSpeedSignal;
        private Signal<float> _causticsSplitSignal;
        private Signal<Color> _shallowColorSignal;
        private Signal<Color> _deepColorSignal;
        private Signal<float> _absorptionSignal;
        private Effect _configReaction;

        [MenuItem("Tools/Snm/Water System")]
        private static void Open()
        {
            var window = GetWindow<WaterSystemTestWindow>();
            window.titleContent = new GUIContent("Water System");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
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

            var runButton = new Button { text = "\u25b6 Run" };
            runButton.clicked += Run;

            var stopButton = new Button { text = "\u25a0 Stop" };
            stopButton.clicked += Stop;

            toolbar.Add(runButton);
            toolbar.Add(stopButton);

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

            if (waterSurfaceShader == null && waterSurfaceMaterial == null)
            {
                Debug.LogError("Assign a water surface shader or material.");
                return;
            }

            // Create material — keep reference so SurfaceInstaller uses it directly
            _ownsMaterial = waterSurfaceMaterial == null;
            _material = _ownsMaterial ? new Material(waterSurfaceShader) : waterSurfaceMaterial;

            _config = new WaterConfig
            {
                surface =
                {
                    waterSurfaceMaterial = _material,
                    size = surfaceSize,
                    autoGenerateMesh = true,
                },
                reflection =
                {
                    enabled = reflectionEnabled,
                    textureWidth = reflectionTextureWidth,
                },
                caustics =
                {
                    enabled = causticsEnabled,
                    causticsTexture = causticsTexture,
                    strength = causticsStrength,
                    scale = causticsScale,
                    speed = causticsSpeed,
                    split = causticsSplit,
                },
                depth =
                {
                    enabled = depthEnabled,
                    shallowColor = shallowColor,
                    deepColor = deepColor,
                    absorption = absorption,
                },
                wave =
                {
                    enabled = waveEnabled,
                    simulationShader = waveSimulationShader,
                    displayShader = waveDisplayShader,
                    textureSize = waveTextureSize,
                }
            };

            _contextGO = new GameObject("[WaterSystemTest]");
            _handle = WaterSystemInstaller.Install(_contextGO, _config, resolvedCamera);

            SetupSignals();

            _simulationContainer = rootVisualElement.Q<VisualElement>("simulation-container");
            _simulationContainer.Clear();

            // Settings panel
            var settingsPanel = CreateSettingsPanel();
            _simulationContainer.Add(settingsPanel);

            // Texture previews
            AddTexturePreview("Reflection Output", _handle.ReflectionTexture, out _reflectionImage);
            AddTexturePreview("Wave Display", _handle.WaveDisplayTexture, out _waveImage);

            // Info label
            var infoLabel = new Label("Navigate Scene View to update. Use Apply Settings for live tweaking.")
            {
                style =
                {
                    marginTop = 4,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    color = new Color(0.6f, 0.6f, 0.6f)
                }
            };
            _simulationContainer.Add(infoLabel);
        }

        private void AddTexturePreview(string label, Texture texture, out Image imageOut)
        {
            imageOut = null;
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
            imageOut = img;
        }

        private void SetupSignals()
        {
            _causticsTextureSignal = new Signal<Texture2D>(causticsTexture);
            _causticsStrengthSignal = new Signal<float>(causticsStrength);
            _causticsScaleSignal = new Signal<float>(causticsScale);
            _causticsSpeedSignal = new Signal<float>(causticsSpeed);
            _causticsSplitSignal = new Signal<float>(causticsSplit);

            _shallowColorSignal = new Signal<Color>(shallowColor);
            _deepColorSignal = new Signal<Color>(deepColor);
            _absorptionSignal = new Signal<float>(absorption);

            _configReaction = new Effect(() =>
            {
                if (_config == null) return;

                _config.caustics.causticsTexture = _causticsTextureSignal.Value;
                _config.caustics.strength = _causticsStrengthSignal.Value;
                _config.caustics.scale = _causticsScaleSignal.Value;
                _config.caustics.speed = _causticsSpeedSignal.Value;
                _config.caustics.split = _causticsSplitSignal.Value;

                _config.depth.shallowColor = _shallowColorSignal.Value;
                _config.depth.deepColor = _deepColorSignal.Value;
                _config.depth.absorption = _absorptionSignal.Value;
            });
        }

        private VisualElement CreateSettingsPanel()
        {
            var panel = new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f),
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 4,
                    paddingBottom = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    borderTopRightRadius = 4,
                    borderTopLeftRadius = 4,
                }
            };

            var applyBtn = new Button { text = "Apply Settings", style = { marginTop = 4 } };
            applyBtn.clicked += ApplySettings;
            panel.Add(applyBtn);

            return panel;
        }

        private void Stop()
        {
            if (_simulationContainer != null)
            {
                _simulationContainer.Clear();
            }

            _reflectionImage = null;
            _waveImage = null;

            _configReaction?.Dispose();
            _configReaction = null;

            _handle?.Dispose();
            _handle = null;
            _config = null;

            if (_contextGO != null)
            {
                DestroyImmediate(_contextGO);
                _contextGO = null;
            }

            if (_ownsMaterial && _material != null)
            {
                DestroyImmediate(_material);
            }
            _material = null;
        }

        private void ApplySettings()
        {
            if (_causticsStrengthSignal == null) return;

            _causticsTextureSignal.Value = causticsTexture;
            _causticsStrengthSignal.Value = causticsStrength;
            _causticsScaleSignal.Value = causticsScale;
            _causticsSpeedSignal.Value = causticsSpeed;
            _causticsSplitSignal.Value = causticsSplit;

            _shallowColorSignal.Value = shallowColor;
            _deepColorSignal.Value = deepColor;
            _absorptionSignal.Value = absorption;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            var halfX = surfaceSize.x * 0.5f;
            var halfZ = surfaceSize.y * 0.5f;

            var c0 = new Vector3(-halfX, 0, -halfZ);
            var c1 = new Vector3(-halfX, 0, halfZ);
            var c2 = new Vector3(halfX, 0, halfZ);
            var c3 = new Vector3(halfX, 0, -halfZ);

            Handles.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            Handles.DrawLine(c0, c1);
            Handles.DrawLine(c1, c2);
            Handles.DrawLine(c2, c3);
            Handles.DrawLine(c3, c0);
            Handles.DrawLine(c0, c2);
            Handles.DrawLine(c1, c3);

            // Draw normal arrow
            Handles.color = new Color(0.2f, 0.6f, 1f, 0.5f);
            Handles.ArrowHandleCap(0, Vector3.zero, Quaternion.LookRotation(Vector3.up), 1f, EventType.Repaint);
        }
    }
}

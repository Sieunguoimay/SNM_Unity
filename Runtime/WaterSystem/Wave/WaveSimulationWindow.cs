using Snm.DependencyInjection;
using Snm.Reactivity;
using Snm.Runtime.Dispose;
using Snm.Runtime.Unity;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.WaterSystem.Wave
{
    public class WaveSimulationWindow : EditorWindow
    {
        [Header("Disturbance Settings")]
        [SerializeField] private float radius = 0.02f;
        [SerializeField] private float strength = 0.01f;

        [Header("Simulation Settings")]
        [SerializeField] private float damping = 0.97f;
        [Range(0.01f, 0.5f)]
        [SerializeField] private float waveSpeed = 0.5f;
        [Tooltip("Multiplier for wave propagation speed. Higher = faster waves.")]
        [Range(0.1f, 10.0f)]
        [SerializeField] private float waveSpreadSpeed = 5.0f;

        [Header("Display Settings")]
        [SerializeField] private float heightfieldStrength = 1.0f;

        [Header("Interaction")]
        [SerializeField] private bool continuousDrag = true;
        [SerializeField] private float dragSpacing = 0.01f;

        private static readonly string[] DisplayModeLabels = { "Height", "Normal", "Heightfield" };

        private RuntimeContainer _scope;
        private IWaveSimulation _simulation;
        private Image _waveImage;
        private Label _waveDisplayLabel;
        private VisualElement _simulationContainer;

        // Signals for reactive config binding
        private Signal<float> _dampingSignal;
        private Signal<float> _waveSpeedSignal;
        private Signal<float> _waveSpreadSpeedSignal;
        private Signal<float> _heightfieldStrengthSignal;
        private Signal<int> _displayModeSignal;
        private Effect _configReaction;

        private bool _isDragging;
        private Vector2 _lastDropUV;
        private Editor _settingsEditor;

        [MenuItem("Tools/Snm/Water Wave")]
        private static void Open()
        {
            var window = GetWindow<WaveSimulationWindow>();
            window.titleContent = new GUIContent("Wave Simulation");
            window.minSize = new Vector2(400, 500);
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

            var runButton = new Button { text = "▶ Run" };
            runButton.clicked += Run;

            var stopButton = new Button { text = "■ Stop" };
            stopButton.clicked += Stop;

            toolbar.Add(runButton);
            toolbar.Add(stopButton);

            _settingsEditor = Editor.CreateEditor(this);
            var inspector = new IMGUIContainer(() => { _settingsEditor.OnInspectorGUI(); }) { style = { flexShrink = 1, flexGrow = 0 } };
            var scrollView = new ScrollView();
            scrollView.Add(inspector);
            scrollView.Add(toolbar);
            scrollView.Add(new VisualElement { name = "simulation-container" });
            rootVisualElement.Add(scrollView);
        }

        private void Run()
        {
            Stop();

            var waveSimulationShader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/SNM_Unity/Runtime/WaterSystem/Wave/WaveSimulation.shader");
            var displayShader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/SNM_Unity/Runtime/WaterSystem/Wave/WaveDisplay.shader");

            var updater = new GameObject("WaveSimulationUpdater").AddComponent<UpdateDispatcher>();

            var builder = new ContainerBuilder();

            builder.Bind<IUpdateService>().ToInstance(updater);

            WaveSimulationInstaller.Install(builder, 512, waveSimulationShader, displayShader);

            // Register updater GameObject cleanup
            builder.Bind<DisposeCallback>().ToScoped(_ =>
                new DisposeCallback(() => UnityEngineUtility.DestroyObject(updater.gameObject)));

            _scope = builder.Build();
            _simulation = _scope.Resolve<IWaveSimulation>();
            // Force DisposeCallback creation so it's tracked for disposal
            _scope.Resolve<DisposeCallback>();

            // Set up reactive config binding
            SetupSignals();

            _simulationContainer = rootVisualElement.Q<VisualElement>("simulation-container");
            _simulationContainer.Clear();

            // Settings panel
            var settingsPanel = CreateSettingsPanel();
            _simulationContainer.Add(settingsPanel);

            // Wave display label
            _waveDisplayLabel = new Label("Wave Display - " + DisplayModeLabels[_displayModeSignal.Value])
            {
                style = { unityTextAlign = TextAnchor.MiddleCenter, color = new Color(0.7f, 0.7f, 0.7f) }
            };
            _simulationContainer.Add(_waveDisplayLabel);

            // Wave display image
            _waveImage = new Image
            {
                image = _simulation.GetDisplayTexture(),
                style =
                {
                    flexGrow = 1,
                    minHeight = 300,
                    marginTop = 4
                }
            };
            _waveImage.RegisterCallback<MouseDownEvent>(OnMouseDown);
            _waveImage.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            _waveImage.RegisterCallback<MouseUpEvent>(OnMouseUp);
            _waveImage.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            _simulationContainer.Add(_waveImage);

            // Info label
            var infoLabel = new Label("Click and drag on the image to create waves")
            {
                style =
                {
                    marginTop = 4,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    color = new Color(0.6f, 0.6f, 0.6f)
                }
            };
            _simulationContainer.Add(infoLabel);

            // Repaint schedule
            _waveImage.schedule.Execute(_ => _waveImage.MarkDirtyRepaint()).Every(50);
        }

        private void SetupSignals()
        {
            _dampingSignal = new Signal<float>(damping);
            _waveSpeedSignal = new Signal<float>(waveSpeed);
            _waveSpreadSpeedSignal = new Signal<float>(waveSpreadSpeed);
            _heightfieldStrengthSignal = new Signal<float>(heightfieldStrength);
            _displayModeSignal = new Signal<int>(0);

            // Reaction: signals -> config (runs once on creation + on any signal change)
            _configReaction = new Effect(() =>
            {
                if (_simulation == null) return;

                var config = _simulation.Config;
                config.damping = _dampingSignal.Value;
                config.waveSpeed = _waveSpeedSignal.Value;
                config.waveSpreadSpeed = _waveSpreadSpeedSignal.Value;
                config.heightfieldStrength = _heightfieldStrengthSignal.Value;
                config.displayMode = _displayModeSignal.Value;
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

            // Quick action buttons
            var buttonRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, marginTop = 4 }
            };

            var randomDropBtn = new Button { text = "Random Drop" };
            randomDropBtn.clicked += AddRandomDisturbance;

            var clearBtn = new Button { text = "Clear Waves" };
            clearBtn.clicked += ClearWaves;

            var centerDropBtn = new Button { text = "Center Drop" };
            centerDropBtn.clicked += () => AddDisturbance(new Vector2(0.5f, 0.5f));

            var toggleModeBtn = new Button { text = "Toggle Display Mode" };
            toggleModeBtn.clicked += ToggleDisplayMode;

            buttonRow.Add(randomDropBtn);
            buttonRow.Add(centerDropBtn);
            buttonRow.Add(clearBtn);
            buttonRow.Add(toggleModeBtn);
            panel.Add(buttonRow);

            // Apply settings button
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

            if (_waveImage != null)
            {
                _waveImage.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                _waveImage.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                _waveImage.UnregisterCallback<MouseUpEvent>(OnMouseUp);
                _waveImage.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
                _waveImage = null;
            }

            _waveDisplayLabel = null;

            _configReaction?.Dispose();
            _configReaction = null;

            _scope?.Dispose();
            _scope = null;
            _simulation = null;
        }

        #region Mouse Interaction

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (_simulation == null) return;

            _isDragging = true;

            var uv = GetUVFromMouse(evt.localMousePosition, _waveImage.contentRect);
            AddDisturbance(uv);
            _lastDropUV = uv;
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!_isDragging || _simulation == null) return;

            var uv = GetUVFromMouse(evt.localMousePosition, _waveImage.contentRect);

            if (continuousDrag)
            {
                var distance = Vector2.Distance(uv, _lastDropUV);

                if (distance >= dragSpacing)
                {
                    int steps = Mathf.CeilToInt(distance / dragSpacing);
                    for (int i = 1; i <= steps; i++)
                    {
                        var t = i / (float)steps;
                        var interpolatedUV = Vector2.Lerp(_lastDropUV, uv, t);
                        AddDisturbance(interpolatedUV);
                    }
                    _lastDropUV = uv;
                }
            }
            else
            {
                AddDisturbance(uv);
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            _isDragging = false;
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            _isDragging = false;
        }

        private Vector2 GetUVFromMouse(Vector2 localPosition, Rect contentRect)
        {
            var imageRect = GetImageRect(contentRect);

            var uv = new Vector2(
                (localPosition.x - imageRect.x) / imageRect.width,
                1f - (localPosition.y - imageRect.y) / imageRect.height
            );

            return uv;
        }

        private Rect GetImageRect(Rect contentRect)
        {
            int textureWidth = 512;
            int textureHeight = 512;

            if (_simulation != null)
            {
                var displayTex = _simulation.GetDisplayTexture();
                if (displayTex != null)
                {
                    textureWidth = displayTex.width;
                    textureHeight = displayTex.height;
                }
            }

            float contentAspect = contentRect.width / contentRect.height;
            float textureAspect = (float)textureWidth / textureHeight;

            Rect imageRect = contentRect;

            if (contentAspect > textureAspect)
            {
                float scaledWidth = contentRect.height * textureAspect;
                imageRect.x = contentRect.x + (contentRect.width - scaledWidth) * 0.5f;
                imageRect.width = scaledWidth;
            }
            else
            {
                float scaledHeight = contentRect.width / textureAspect;
                imageRect.y = contentRect.y + (contentRect.height - scaledHeight) * 0.5f;
                imageRect.height = scaledHeight;
            }

            return imageRect;
        }

        #endregion

        #region Disturbance Methods

        private void AddDisturbance(Vector2 uv)
        {
            if (_simulation == null) return;

            uv.x = Mathf.Clamp01(uv.x);
            uv.y = Mathf.Clamp01(uv.y);

            _simulation.AddDisturbance(new WaveDisturbance
            {
                uvPos = uv,
                radius = radius,
                strength = strength
            });
        }

        private void AddRandomDisturbance()
        {
            var uv = new Vector2(
                Random.Range(0.1f, 0.9f),
                Random.Range(0.1f, 0.9f)
            );
            AddDisturbance(uv);
        }

        private void ToggleDisplayMode()
        {
            if (_displayModeSignal == null) return;

            _displayModeSignal.Value = (_displayModeSignal.Value + 1) % 3;

            if (_waveDisplayLabel != null)
                _waveDisplayLabel.text = "Wave Display - " + DisplayModeLabels[_displayModeSignal.Value];
        }

        private void ClearWaves()
        {
            _simulation?.ClearSimulation();
        }

        private void ApplySettings()
        {
            if (_dampingSignal == null) return;

            // Push editor values into signals -> reaction auto-updates config
            _dampingSignal.Value = damping;
            _waveSpeedSignal.Value = waveSpeed;
            _waveSpreadSpeedSignal.Value = waveSpreadSpeed;
            _heightfieldStrengthSignal.Value = heightfieldStrength;
        }

        #endregion
    }
}

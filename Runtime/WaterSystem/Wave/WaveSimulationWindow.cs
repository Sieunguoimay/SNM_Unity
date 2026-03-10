using Snm.Runtime.Dispose;
using Snm.Runtime.Unity;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

namespace Snm.WaterSystem.Wave
{
    public class WaveSimulationWindow : EditorWindow
    {
        [Header("Disturbance Settings")]
        [SerializeField] private float radius = 0.05f;
        [SerializeField] private float strength = 0.15f;

        [Header("Simulation Settings")]
        [SerializeField] private float damping = 0.99f;
        [SerializeField] private float waveSpeed = 0.4f;

        [Header("Interaction")]
        [SerializeField] private bool continuousDrag = true;
        [SerializeField] private float dragSpacing = 0.02f;

        private DisposeCollection _disposable;
        private WaveSimulationRenderer _renderer;
        private Image _waveImage;
        private VisualElement _simulationContainer;

        private bool _isDragging;
        private Vector2 _lastDropUV;
        private float _dragAccumulator;

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
            toolbar.Add(new VisualElement { style = { flexGrow = 1 } });

            rootVisualElement.Add(toolbar);
            rootVisualElement.Add(new VisualElement { name = "simulation-container" });
        }

        private void Run()
        {
            Stop();

            var waveSimulationShader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/SNM_Unity/Runtime/WaterSystem/Wave/WaveSimulation.shader");

            var renderTexture = CreateRenderTexture(512);
            var updater = new GameObject("WaveSimulationUpdater").AddComponent<UpdateDispatcher>();
            var updaterDispose = new DisposeCallback(() => { UnityEngineUtility.DestroyObject(updater.gameObject); });

            _renderer = new WaveSimulationRenderer(renderTexture, waveSimulationShader, updater)
            {
                damping = damping,
                waveSpeed = waveSpeed
            };

            _disposable = new DisposeCollection(updaterDispose, _renderer);

            _simulationContainer = rootVisualElement.Q<VisualElement>("simulation-container");
            _simulationContainer.Clear();

            // Settings panel
            var settingsPanel = CreateSettingsPanel();
            _simulationContainer.Add(settingsPanel);

            // Wave visualization image
            _waveImage = new Image
            {
                image = renderTexture,
                style =
                {
                    flexGrow = 1,
                    minHeight = 300,
                    marginTop = 8
                }
            };

            // Mouse interaction callbacks
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

            // Inspector for serialized settings
            var inspector = new InspectorElement(Editor.CreateEditor(this));
            panel.Add(inspector);

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

            buttonRow.Add(randomDropBtn);
            buttonRow.Add(centerDropBtn);
            buttonRow.Add(clearBtn);
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

            _disposable?.Dispose();
            _disposable = null;
            _renderer = null;
        }

        #region Mouse Interaction

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (_renderer == null) return;

            _isDragging = true;
            _dragAccumulator = 0f;

            var uv = GetUVFromMouse(evt.localMousePosition, _waveImage.contentRect);
            AddDisturbance(uv);
            _lastDropUV = uv;
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!_isDragging || _renderer == null) return;

            var uv = GetUVFromMouse(evt.localMousePosition, _waveImage.contentRect);

            if (continuousDrag)
            {
                var distance = Vector2.Distance(uv, _lastDropUV);

                // Interpolate drops along the drag path
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
            // Handle aspect ratio preservation
            var imageRect = GetImageRect(contentRect);

            // Convert to UV relative to the displayed image
            var uv = new Vector2(
                (localPosition.x - imageRect.x) / imageRect.width,
                1f - (localPosition.y - imageRect.y) / imageRect.height // Flip Y
            );

            return uv;
        }

        private Rect GetImageRect(Rect contentRect)
        {
            // Simple case: image fills the content rect
            // For proper aspect ratio handling, you'd need texture dimensions
            return contentRect;
        }

        #endregion

        #region Disturbance Methods

        private void AddDisturbance(Vector2 uv)
        {
            if (_renderer == null) return;

            // Clamp UV to valid range
            uv.x = Mathf.Clamp01(uv.x);
            uv.y = Mathf.Clamp01(uv.y);

            _renderer.AddDisturbance(new WaveDisturbance
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

        private void ClearWaves()
        {
            if (_renderer == null) return;
            // Re-create render textures to clear
            Stop();
            Run();
        }

        private void ApplySettings()
        {
            if (_renderer != null)
            {
                _renderer.damping = damping;
                _renderer.waveSpeed = waveSpeed;
            }
        }

        #endregion

        public static RenderTexture CreateRenderTexture(int size)
        {
            var desc = new RenderTextureDescriptor(size, size)
            {
                graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
                depthBufferBits = 0,
                msaaSamples = 1,
                sRGB = false,
                enableRandomWrite = false,
            };

            // Use new RenderTexture instead of GetTemporary for persistent use
            var rt = new RenderTexture(desc);
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.useMipMap = false;
            rt.autoGenerateMips = false;
            rt.Create();

            // Clear to black
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = prev;

            return rt;
        }
    }
}

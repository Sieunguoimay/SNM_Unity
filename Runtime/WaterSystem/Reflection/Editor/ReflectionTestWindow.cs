using Snm.DependencyInjection;
using Snm.Reactivity;
using Snm.Runtime.Dispose;
using Snm.Runtime.Unity;
using Snm.WaterSystem.Surface;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.WaterSystem.Reflection
{
    public class ReflectionTestWindow : EditorWindow
    {
        [Header("Water Surface")]
        [SerializeField] private Vector3 surfacePosition = Vector3.zero;
        [SerializeField] private Vector3 surfaceRotationEuler = Vector3.zero;
        [SerializeField] private Vector2 surfaceSize = new(10f, 10f);

        [Header("Camera")]
        [SerializeField] private Camera sourceCamera;

        [Header("Reflection Settings")]
        [SerializeField] private int textureWidth = 256;

        private RuntimeContainer _scope;
        private ReflectionFeature _feature;
        private SurfaceData _surfaceData;
        private Image _reflectionImage;
        private VisualElement _simulationContainer;
        private Editor _settingsEditor;

        // Signals for reactive config
        private Signal<Vector3> _surfacePositionSignal;
        private Signal<Vector3> _surfaceRotationSignal;
        private Signal<Vector2> _surfaceSizeSignal;
        private Effect _configReaction;

        [MenuItem("Tools/Snm/Water Reflection")]
        private static void Open()
        {
            var window = GetWindow<ReflectionTestWindow>();
            window.titleContent = new GUIContent("Reflection Test");
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
            var updater = new GameObject("ReflectionTestUpdater").AddComponent<UpdateDispatcher>();

            _surfaceData = new SurfaceData
            {
                position = surfacePosition,
                rotation = Quaternion.Euler(surfaceRotationEuler),
                size = surfaceSize
            };

            var dummyMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            var config = new WaterConfig { reflection = { textureWidth = textureWidth } };

            var ctx = new WaterFeatureContext(config, _surfaceData, dummyMaterial, resolvedCamera);

            var builder = new ContainerBuilder();
            builder.Bind<IUpdateService>().ToInstance(updater);
            builder.Bind<ReflectionFeature>().ToScoped(_ => ReflectionInstaller.Install(ctx));
            builder.Bind<DisposeCallback>().ToScoped(_ =>
                new DisposeCallback(() =>
                {
                    if(updater) UnityEngineUtility.DestroyObject(updater.gameObject);
                    DestroyImmediate(dummyMaterial);
                }));

            _scope = builder.Build();
            _feature = _scope.Resolve<ReflectionFeature>();
            _scope.Resolve<DisposeCallback>();

            var composite = new WaterFeatureComposite();
            composite.Add(_feature);
            updater.AddUpdateTarget(composite);
            updater.AddLateUpdateTarget(composite);

            SetupSignals();

            _simulationContainer = rootVisualElement.Q<VisualElement>("simulation-container");
            _simulationContainer.Clear();

            // Settings panel
            var settingsPanel = CreateSettingsPanel();
            _simulationContainer.Add(settingsPanel);

            // Reflection output label
            var outputLabel = new Label("Reflection Output")
            {
                style = { unityTextAlign = TextAnchor.MiddleCenter, color = new Color(0.7f, 0.7f, 0.7f) }
            };
            _simulationContainer.Add(outputLabel);

            // Reflection display image
            _reflectionImage = new Image
            {
                image = _feature.Texture,
                style =
                {
                    flexGrow = 1,
                    minHeight = 300,
                    marginTop = 4
                }
            };
            _simulationContainer.Add(_reflectionImage);

            // Info label
            var infoLabel = new Label("Navigate Scene View to update reflection")
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
            _reflectionImage.schedule.Execute(_ => _reflectionImage.MarkDirtyRepaint()).Every(50);
        }

        private void SetupSignals()
        {
            _surfacePositionSignal = new Signal<Vector3>(surfacePosition);
            _surfaceRotationSignal = new Signal<Vector3>(surfaceRotationEuler);
            _surfaceSizeSignal = new Signal<Vector2>(surfaceSize);

            _configReaction = new Effect(() =>
            {
                if (_surfaceData == null) return;

                _surfaceData.position = _surfacePositionSignal.Value;
                _surfaceData.rotation = Quaternion.Euler(_surfaceRotationSignal.Value);
                _surfaceData.size = _surfaceSizeSignal.Value;
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

            _configReaction?.Dispose();
            _configReaction = null;

            _scope?.Dispose();
            _scope = null;
            _feature = null;
            _surfaceData = null;
        }

        private void ApplySettings()
        {
            if (_surfacePositionSignal == null) return;

            _surfacePositionSignal.Value = surfacePosition;
            _surfaceRotationSignal.Value = surfaceRotationEuler;
            _surfaceSizeSignal.Value = surfaceSize;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            var rotation = Quaternion.Euler(surfaceRotationEuler);
            var right = rotation * Vector3.right * surfaceSize.x * 0.5f;
            var forward = rotation * Vector3.forward * surfaceSize.y * 0.5f;
            var center = surfacePosition;

            var c0 = center - right - forward;
            var c1 = center - right + forward;
            var c2 = center + right + forward;
            var c3 = center + right - forward;

            Handles.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            Handles.DrawLine(c0, c1);
            Handles.DrawLine(c1, c2);
            Handles.DrawLine(c2, c3);
            Handles.DrawLine(c3, c0);
            Handles.DrawLine(c0, c2);
            Handles.DrawLine(c1, c3);

            // Draw normal arrow
            var normal = rotation * Vector3.up;
            Handles.color = new Color(0.2f, 0.6f, 1f, 0.5f);
            Handles.ArrowHandleCap(0, center, Quaternion.LookRotation(normal), 1f, EventType.Repaint);
        }
    }
}

using Snm.DependencyInjection;
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
        [SerializeField] private WaveSimulationConfig config;

        private IWaveSimulation _simulation;

        private WaveSimulationView _view;
        private DisposeCollection _dispose;

        [MenuItem("Tools/Snm/Water Wave")]
        static void Open()
        {
            var window = GetWindow<WaveSimulationWindow>();
            window.titleContent = new GUIContent("Wave Simulation");
        }

        void CreateGUI()
        {
            var editor = Editor.CreateEditor(this);
            var editorVE = new InspectorElement(editor) { style = { flexShrink = 0, flexGrow = 0 } };
            editorVE.Bind(editor.serializedObject);

            var toolbar = new Toolbar();

            var run = new Button(CreateTestSimulation) { text = "Run Test Simulation" };
            var stop = new Button(Stop) { text = "Stop" };

            toolbar.Add(run);
            toolbar.Add(stop);

            rootVisualElement.Add(editorVE);
            rootVisualElement.Add(toolbar);

            _view = new WaveSimulationView();
            rootVisualElement.Add(_view.Root);
        }

        void CreateTestSimulation()
        {
            Stop();

            var simShader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/SNM_Unity/Runtime/WaterSystem/Wave/WaveSimulation.shader");

            var displayShader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/SNM_Unity/Runtime/WaterSystem/Wave/WaveDisplay.shader");

            var updater = new GameObject("WaveUpdater")
                .AddComponent<UpdateDispatcher>();

            var builder = new ContainerBuilder();

            builder.Bind<IUpdateService>().ToInstance(updater);

            WaveSimulationInstaller.Install(builder, 512, simShader, displayShader);

            var scope = builder.Build();
            _simulation = scope.Resolve<IWaveSimulation>();

            var waveConfig = _simulation.Config;
            waveConfig.damping = config.damping;
            waveConfig.waveSpeed = config.waveSpeed;
            waveConfig.waveSpreadSpeed = config.waveSpreadSpeed;
            waveConfig.waveNormalStrength = config.waveNormalStrength;

            _view.Attach(_simulation);

            _dispose = new DisposeCollection(
                scope, 
                new DisposeCallback(() => UnityEngineUtility.DestroyObject(updater.gameObject)));
        }

        public void AttachSimulation(IWaveSimulation simulation)
        {
            Stop();

            _simulation = simulation;
            _view.Attach(simulation);
        }

        void Stop()
        {
            _view?.Detach();

            _dispose?.Dispose();
            _dispose = null;

            _simulation = null;
        }
    }
}

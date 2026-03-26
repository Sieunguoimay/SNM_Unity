using Snm.Runtime.Unity;
using UnityEditor;
using UnityEngine;

namespace Snm.Runtime.DebugDraw
{
    public sealed class DebugDrawManager : MonoBehaviour
    {
        private static DebugDrawManager _instance;
        private bool _isQuitting;

        internal static ShapeDrawer Shapes { get; private set; }
        internal static LabelDrawer Labels { get; private set; }
        internal static DebugDrawConfig Config { get; private set; }

        private static bool _enabled = true;
        public static bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                Shapes?.SetVisible(value);
                Labels?.SetVisible(value);
            }
        }

        public static DebugDrawManager Instance
        {
            get
            {
                if (_instance == null) Boot();
                return _instance;
            }
        }

        [MenuItem("Tools/Snm/Toggle Debug Draw Manager")]
        private static void CreateMenuItem()
        {
            Enabled = !Enabled;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { UnityEngineUtility.DestroyObject(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        private void Init()
        {
            Config = Resources.Load<DebugDrawConfig>("DebugDrawConfig") ?? ScriptableObject.CreateInstance<DebugDrawConfig>();
            Shapes = new ShapeDrawer(Config, transform);
            Labels = new LabelDrawer(Config, transform);
            Enabled = Config.enabled;
        }

        private void Update()
        {
            if (!Enabled || _isQuitting) return;
            Labels.Tick();
        }

        private void OnApplicationQuit() => _isQuitting = true;

        private void OnDestroy()
        {
            if (_instance != this) return;
            _instance = null;
            Shapes?.Dispose(); Shapes = null;
            Labels?.Dispose(); Labels = null;
        }

        private static void Boot()
        {
            var go = new GameObject("[DebugDraw] Manager") {  };
            go.AddComponent<DebugDrawManager>();
        }
    }
}

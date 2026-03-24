using UnityEngine;

namespace Snm.Runtime.DebugDraw
{
    public sealed class DebugDrawManager : MonoBehaviour
    {
        private static DebugDrawManager _instance;
        private bool _isQuitting;

        internal static ShapeDrawer      Shapes { get; private set; }
        internal static LabelDrawer      Labels { get; private set; }
        internal static DebugDrawConfig  Config { get; private set; }

        public static bool Enabled { get; set; } = true;

        public static DebugDrawManager Instance
        {
            get
            {
                if (_instance == null) Boot();
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        private void Init()
        {
            Config  = Resources.Load<DebugDrawConfig>("DebugDrawConfig") ?? ScriptableObject.CreateInstance<DebugDrawConfig>();
            Enabled = Config.enabled;
            Shapes  = new ShapeDrawer(Config);
            Labels  = new LabelDrawer(Config);
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
            var go = new GameObject("[DebugDraw] Manager") { hideFlags = HideFlags.DontSave };
            go.AddComponent<DebugDrawManager>();
        }
    }
}

using System;
using UnityEngine;

#if UNITY_DEBUG || DEVELOPMENT_BUILD
namespace Snm.Runtime.DebugVisualize
{
    public class DebugVisualizeManager : MonoBehaviour
    {
        private static DebugVisualizeManager _instance;
        public static DebugVisualizeManager Instance => _instance;

        private DebugVisualizeSettings _settings;
        private TextDisplaySystem _textSystem;
        private StatsDisplaySystem _statsSystem;
        private ShapeDrawerSystem _shapeDrawer;

        private bool _enabled = true;
        private bool _isQuitting;

        public static bool IsEnabled
        {
            get => Instance?._enabled ?? false;
            set
            {
                if (Instance != null)
                {
                    Instance._enabled = value;
                }
            }
        }

        public static TextDisplaySystem TextSystem => Instance?._textSystem;
        public static StatsDisplaySystem StatsSystem => Instance?._statsSystem;
        public static ShapeDrawerSystem ShapeDrawer => Instance?._shapeDrawer;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            _settings = Resources.Load<DebugVisualizeSettings>("DebugVisualizeSettings");
            if (_settings == null)
            {
                _settings = ScriptableObject.CreateInstance<DebugVisualizeSettings>();
            }

            _enabled = _settings.Enabled;

            _textSystem = new TextDisplaySystem(_settings);
            _statsSystem = new StatsDisplaySystem(_settings);
            _shapeDrawer = new ShapeDrawerSystem(_settings);
        }

        private void Update()
        {
            if (!_enabled || _isQuitting) return;

            _textSystem?.Update();
            _statsSystem?.Update();
            _shapeDrawer?.Update();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            _textSystem?.Dispose();
            _statsSystem?.Dispose();
            _shapeDrawer?.Dispose();
        }

        public static void EnsureInitialized()
        {
            if (_instance == null)
            {
                var go = new GameObject("DebugVisualizeManager");
                go.hideFlags = HideFlags.HideAndDontSave;
            }
        }
    }
}
#endif

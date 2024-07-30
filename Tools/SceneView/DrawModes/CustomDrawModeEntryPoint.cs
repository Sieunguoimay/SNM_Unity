#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;

namespace SceneViewDrawModes
{
    [InitializeOnLoad]
    public static class CustomDrawModeEntryPoint
    {
        private static SceneView _currentSceneView;
        private static readonly ICustomDrawMode[] _customDrawModes;
        static CustomDrawModeEntryPoint()
        {
            EditorApplication.update += OnUpdateEditor;

            _customDrawModes = new[] {
                new CustomDrawMode_Bone(),
                //Add more here..
            };

            foreach (var m in _customDrawModes)
            {
                SceneView.AddCameraMode(m.Name, "Custom Shading Mode");
            }
        }

        private static void OnUpdateEditor()
        {
            if (SceneView.lastActiveSceneView != _currentSceneView)
            {
                if (_currentSceneView != null)
                {
                    _currentSceneView.onCameraModeChanged -= OnDrawModeChanged;
                }
                if (SceneView.lastActiveSceneView != null)
                {
                    _currentSceneView = SceneView.lastActiveSceneView;
                    _currentSceneView.onCameraModeChanged += OnDrawModeChanged;
                }
            }

            foreach (var d in _customDrawModes)
            {
                if (d.IsActive) d.Update();
            }
        }

        private static void OnDrawModeChanged(SceneView.CameraMode mode)
        {
            var found = _customDrawModes.FirstOrDefault(d => d.Name == mode.name);
            if (found != null)
            {
                found.Setup(_currentSceneView);
            }
            else
            {
                foreach (var d in _customDrawModes)
                {
                    if (d.IsActive) d.TearDown();
                }
            }
        }
    }
}

#endif

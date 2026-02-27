#if UNITY_EDITOR
using UnityEditor;

namespace Snm.Tools.Engine
{
    [InitializeOnLoad]
    public static class ToggleEditorRepaint
    {
        private static bool enabled;

        static ToggleEditorRepaint()
        {
            // Restore menu check state on domain reload
            Menu.SetChecked("Tools/Snm/Toggle/Editor Repaint _F10", enabled);
        }

        [MenuItem("Tools/Snm/Toggle/Editor Repaint _F10")]
        public static void Toggle()
        {
            enabled = !enabled;

            if (enabled)
                EditorApplication.update += Tick;
            else
                EditorApplication.update -= Tick;

            Menu.SetChecked("Tools/Snm/Toggle/Editor Repaint _F10", enabled);
        }

        private static void Tick()
        {
            // Scene View
            SceneView.RepaintAll();

            // Game View (important for shaders using _Time)
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
#endif

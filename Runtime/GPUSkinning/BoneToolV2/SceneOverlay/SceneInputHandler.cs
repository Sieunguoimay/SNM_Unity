#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneToolV2
{
    /// <summary>
    /// Static class that routes SceneView events to the active tool mode.
    /// Handles mode-switching via number keys 1/2/3 and dispatches key-down
    /// events to the current mode.
    /// </summary>
    public static class SceneInputHandler
    {
        /// <summary>
        /// Main entry point: call from SceneView.duringSceneGui callback.
        /// Routes input to the active mode and handles global shortcuts.
        /// </summary>
        /// <param name="view">The current SceneView.</param>
        /// <param name="activeMode">The currently active IToolMode (may be null).</param>
        /// <param name="doc">The RigDocument being edited.</param>
        /// <returns>
        /// A ToolModeEnum if a mode switch was requested via number keys, or null if no switch.
        /// </returns>
        public static RigDocument.ToolModeEnum? HandleSceneInput(SceneView view, IToolMode activeMode, RigDocument doc)
        {
            if (doc == null) return null;

            var e = Event.current;
            RigDocument.ToolModeEnum? requestedMode = null;

            // Handle global keyboard shortcuts
            if (e.type == EventType.KeyDown)
            {
                // Mode switching via number keys
                switch (e.keyCode)
                {
                    case KeyCode.Alpha1:
                        requestedMode = RigDocument.ToolModeEnum.Skeleton;
                        e.Use();
                        return requestedMode;

                    case KeyCode.Alpha2:
                        requestedMode = RigDocument.ToolModeEnum.Paint;
                        e.Use();
                        return requestedMode;

                    case KeyCode.Alpha3:
                        requestedMode = RigDocument.ToolModeEnum.Test;
                        e.Use();
                        return requestedMode;
                }

                // Dispatch key to active mode
                if (activeMode != null && activeMode.OnKeyDown(e.keyCode))
                {
                    e.Use();
                    return null;
                }
            }

            // Let the active mode handle scene GUI
            activeMode?.OnSceneGUI(view);

            return requestedMode;
        }
    }
}
#endif

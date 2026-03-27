#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneToolV2
{
    /// <summary>
    /// Interface for tool modes (Skeleton Edit, Weight Paint, Test Pose).
    /// Each mode handles scene interaction and keyboard input differently.
    /// </summary>
    public interface IToolMode
    {
        string DisplayName { get; }
        void OnEnter(RigDocument doc);
        void OnExit();
        void OnSceneGUI(SceneView view);

        /// <summary>
        /// Called when a key is pressed. Return true if the key was consumed.
        /// </summary>
        bool OnKeyDown(KeyCode key);
    }
}
#endif

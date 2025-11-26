#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.DependencyVisualize
{
    public class AssetDependencyGraphWindow : EditorWindow
    {
        [MenuItem("Tools/AssetDependencyGraphWindow")]
        public static void Open()
        {
            var window = GetWindow<AssetDependencyGraphWindow>("AssetDependencyGraphWindow");
            window.Show();
        }

        private void CreateGUI()
        {
            var graph = new AssetDependencyGraph();
            graph.StretchToParentSize();
            rootVisualElement.Add(graph);
        }
    }
}
#endif
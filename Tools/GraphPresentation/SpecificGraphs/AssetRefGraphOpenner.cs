#if UNITY_EDITOR
using System.Linq;
using UnityEditor;

namespace Snm.Tools.GraphPresentation
{
    public static class AssetRefGraphOpenner
    {
        [MenuItem("Assets/Tools/GraphVisualizerWindow_AssetRefs")]
        public static void OpenAssetRefGraph()
        {
            var obj = Selection.activeObject;
            if (obj == null) return;

            var graphBuilder = new AssetRefGraphBuilder();
            var graph = graphBuilder.CreateGraph(obj, out var assetToNodeDic);
            var nodeToAsset = assetToNodeDic.Select(kv => (kv.Value, kv.Key));

            var graphVEBuilderConfig = Component_AssetRefGraphVEBuilder.SerializeConfig(nodeToAsset);
            var graphLayoutConfig = Component_SimpleGraphLayout.SerializeConfig(new UnityEngine.Vector2(250f, 70f));

            EditorWindow.GetWindow<GraphVisualizerWindow>().LoadGraph(graph,
                GraphVisualizerComponent<IGraphVEBuilder>.Create<Component_AssetRefGraphVEBuilder>(graphVEBuilderConfig),
                GraphVisualizerComponent<IGraphLayout>.Create<Component_SimpleGraphLayout>(graphLayoutConfig));
        }
    }
}
#endif
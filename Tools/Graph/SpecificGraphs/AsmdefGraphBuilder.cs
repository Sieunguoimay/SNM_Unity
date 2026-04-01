#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Snm.Tools.GraphPresentation
{
    public static class AsmdefGraphBuilder
    {
        private static readonly Color ColorRuntime = new(0.35f, 0.55f, 0.75f);
        private static readonly Color ColorEditor = new(0.55f, 0.45f, 0.7f);
        private static readonly Color ColorTests = new(0.5f, 0.65f, 0.45f);

        public static Graph CreateGraph(out Dictionary<Object, Node> outNodes)
        {
            var guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");
            var nodes = new Dictionary<Object, Node>();
            var connections = new List<Connection>();

            // Create nodes for project assemblies only
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;

                var asmdef = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                if (asmdef == null) continue;

                var asmData = ParseAsmdef(path);

                var node = new Node
                {
                    name = asmData.name ?? asmdef.name,
                    color = ClassifyColor(asmData, path),
                    tooltip = path,
                };
                nodes[asmdef] = node;
            }

            // Create connections
            foreach (var kvp in nodes)
            {
                var asmdef = kvp.Key as AssemblyDefinitionAsset;
                var fromNode = kvp.Value;
                var path = AssetDatabase.GetAssetPath(asmdef);
                var asmData = ParseAsmdef(path);

                if (asmData.references == null) continue;

                foreach (var refStr in asmData.references)
                {
                    var refGuid = refStr.Replace("GUID:", "");
                    var refPath = AssetDatabase.GUIDToAssetPath(refGuid);
                    if (string.IsNullOrEmpty(refPath)) continue;

                    var refAsmdef = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(refPath);
                    if (refAsmdef == null || !nodes.TryGetValue(refAsmdef, out var toNode)) continue;

                    connections.Add(new Connection { from = fromNode.id, to = toNode.id });
                }
            }

            outNodes = nodes;
            return new Graph
            {
                nodes = nodes.Values.ToArray(),
                connections = connections.ToArray()
            };
        }

        private static Color ClassifyColor(AsmdefData data, string path)
        {
            var pathLower = path.ToLowerInvariant();
            var nameLower = (data.name ?? "").ToLowerInvariant();

            if (nameLower.Contains("test") || pathLower.Contains("test"))
                return ColorTests;

            if (data.includePlatforms != null && data.includePlatforms.Any(p => p == "Editor"))
                return ColorEditor;

            if (pathLower.Contains("/editor/") || pathLower.Contains("/editor."))
                return ColorEditor;

            return ColorRuntime;
        }

        private static AsmdefData ParseAsmdef(string path)
        {
            var json = File.ReadAllText(path);
            var data = new AsmdefData();
            EditorJsonUtility.FromJsonOverwrite(json, data);
            return data;
        }

        [System.Serializable]
        private class AsmdefData
        {
            public string name;
            public string[] references;
            public string[] includePlatforms;
        }

        [MenuItem("Tools/Snm/Graph/Assembly References")]
        public static void Open()
        {
            var graph = CreateGraph(out var assetToNode);
            var nodeToAsset = assetToNode.Select(kv => (kv.Value, kv.Key));

            var veConfig = Component_AssetRefGraphVEBuilder.SerializeConfig(nodeToAsset);
            var layoutConfig = Component_SimpleGraphLayout.SerializeConfig(new Vector2(200f, 60f));

            var window = EditorWindow.GetWindow<GraphVisualizerWindow>("Assembly References");
            window.LoadGraph(graph,
                GraphVisualizerComponent<IGraphVEBuilder>.Create<Component_AssetRefGraphVEBuilder>(veConfig),
                GraphVisualizerComponent<IGraphLayout>.Create<Component_SimpleGraphLayout>(layoutConfig));
        }
    }
}
#endif

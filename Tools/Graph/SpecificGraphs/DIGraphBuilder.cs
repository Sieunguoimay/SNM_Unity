#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools.GraphPresentation
{
    public static class DIGraphBuilder
    {
        private static readonly Color ColorModule = new(0.45f, 0.55f, 0.7f);
        private static readonly Color ColorService = new(0.5f, 0.7f, 0.5f);
        private static readonly Color ColorInterface = new(0.7f, 0.6f, 0.4f);

        private static readonly Regex BindRegex = new(@"\.Bind<([^>]+)>", RegexOptions.Compiled);
        private static readonly Regex ResolveRegex = new(@"\.Resolve<([^>]+)>", RegexOptions.Compiled);
        private static readonly Regex ToFactoryRegex = new(@"\.ToFactory\(", RegexOptions.Compiled);
        private static readonly Regex ToInstanceRegex = new(@"\.ToInstance\(", RegexOptions.Compiled);

        public static Graph CreateGraph()
        {
            var nodes = new Dictionary<string, Node>();
            var connections = new List<Connection>();
            var moduleFiles = FindModuleFiles();

            foreach (var filePath in moduleFiles)
            {
                var source = File.ReadAllText(filePath);
                var moduleName = Path.GetFileNameWithoutExtension(filePath);

                // Create module node
                var moduleNode = GetOrCreateNode(nodes, moduleName, ColorModule);
                moduleNode.tooltip = filePath;

                // Find all Bind<T> calls
                var bindings = ExtractBindings(source);

                foreach (var binding in bindings)
                {
                    // Create service node
                    var serviceNode = GetOrCreateNode(nodes, binding.BoundType, ColorService);

                    // Module -> provides -> Service
                    connections.Add(new Connection { from = moduleNode.id, to = serviceNode.id });

                    // Find what this binding resolves (dependencies)
                    foreach (var dep in binding.Dependencies)
                    {
                        var depNode = GetOrCreateNode(nodes, dep, ColorService);
                        // Service -> depends on -> Dependency
                        connections.Add(new Connection { from = serviceNode.id, to = depNode.id });
                    }
                }
            }

            // Deduplicate connections
            var uniqueConnections = connections
                .GroupBy(c => $"{c.from}->{c.to}")
                .Select(g => g.First())
                .Where(c => c.from != c.to)
                .ToArray();

            return new Graph
            {
                nodes = nodes.Values.ToArray(),
                connections = uniqueConnections
            };
        }

        private static Node GetOrCreateNode(Dictionary<string, Node> nodes, string name, Color color)
        {
            if (!nodes.TryGetValue(name, out var node))
            {
                node = new Node { name = name, color = color };
                nodes[name] = node;
            }
            return node;
        }

        private static List<BindingInfo> ExtractBindings(string source)
        {
            var bindings = new List<BindingInfo>();

            var bindMatches = BindRegex.Matches(source);
            foreach (Match bindMatch in bindMatches)
            {
                var boundType = bindMatch.Groups[1].Value;
                var binding = new BindingInfo { BoundType = SimplifyTypeName(boundType) };

                // Find the factory/instance block after this Bind call
                var afterBind = source.Substring(bindMatch.Index);

                // Find end of this binding statement chain (next Bind or end of method)
                var nextBindIdx = afterBind.IndexOf(".Bind<", bindMatch.Length, StringComparison.Ordinal);
                var block = nextBindIdx > 0 ? afterBind.Substring(0, nextBindIdx) : afterBind;

                // Extract Resolve<T> calls within this binding's factory
                if (ToFactoryRegex.IsMatch(block))
                {
                    var resolveMatches = ResolveRegex.Matches(block);
                    foreach (Match resolveMatch in resolveMatches)
                    {
                        binding.Dependencies.Add(SimplifyTypeName(resolveMatch.Groups[1].Value));
                    }
                }

                bindings.Add(binding);
            }

            return bindings;
        }

        private static string SimplifyTypeName(string typeName)
        {
            // Remove namespace prefix if present
            var lastDot = typeName.LastIndexOf('.');
            return lastDot >= 0 ? typeName.Substring(lastDot + 1) : typeName;
        }

        private static List<string> FindModuleFiles()
        {
            var results = new List<string>();
            var guids = AssetDatabase.FindAssets("t:Script");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs")) continue;

                var source = File.ReadAllText(path);
                // Look for classes that extend AppModuleAsset or implement IAppModule
                if (source.Contains(": AppModuleAsset") || source.Contains(": IAppModule"))
                {
                    if (BindRegex.IsMatch(source))
                        results.Add(path);
                }
            }

            return results;
        }

        private class BindingInfo
        {
            public string BoundType;
            public List<string> Dependencies = new();
        }

        [MenuItem("Tools/Snm/Graph/DI Container")]
        public static void Open()
        {
            var graph = CreateGraph();

            var layoutConfig = Component_SimpleGraphLayout.SerializeConfig(new Vector2(200f, 80f));

            var window = EditorWindow.GetWindow<GraphVisualizerWindow>("DI Container");
            window.LoadGraph(graph,
                graphLayoutComponent: GraphVisualizerComponent<IGraphLayout>.Create<Component_SimpleGraphLayout>(layoutConfig));
        }
    }
}
#endif

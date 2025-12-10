#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.GraphPresentation
{
    public class Component_AssetRefGraphVEBuilder : IGraphVEBuilder
    {
        private readonly string config;

        public Component_AssetRefGraphVEBuilder(string config)
        {
            this.config = config;
        }

        VisualElement IGraphVEBuilder.CreateGraphVE(Graph graph)
        {
            var nodeToAssetDic = DeserializeConfig(graph, config)
                .ToDictionary(s => s.node, s => s.asset);
            return GraphVEBuilder.BuildGraphVE(graph, new AssetRefNodeVEBuilder(n => nodeToAssetDic[n]));
        }

        public static IEnumerable<(Node node, UnityEngine.Object asset)> DeserializeConfig(Graph graph, string config)
        {
            return config
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(s =>
                {
                    var pair = s.Split("::", StringSplitOptions.RemoveEmptyEntries);
                    return (id: pair[0], guid: pair[1]);
                })
                .Select(s => (graph.nodes.First(n => n.id == s.id), AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(s.guid))));
        }

        public static string SerializeConfig(IEnumerable<(Node node, UnityEngine.Object asset)> assetToNodeDic)
        {
            return string.Join("|", assetToNodeDic.Select(kv => $"{kv.node.id}::{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(kv.asset))}"));
        }
    }
}
#endif
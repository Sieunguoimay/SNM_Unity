#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Snm.Tools.GraphPresentation
{
    public class AssetRefGraphBuilder
    {
        public Graph CreateGraph(Object target, out Dictionary<Object, Node> outNodes)
        {
            var nodes = new Dictionary<Object, Node>();
            var connections = new Dictionary<string, Connection>();
            CreateNodesAndConnections(target, nodes, connections);
            outNodes = nodes;
            return new Graph()
            {
                nodes = nodes.Values.ToArray(),
                connections = connections.Values.ToArray()
            };
        }

        private static void CreateNodesAndConnections(
            Object target,
            Dictionary<Object, Node> nodes,
            Dictionary<string, Connection> connections)
        {
            if (target == null)
                return;

            var node = TryGetNode(target, nodes);

            foreach (var a in AssetReferenceExtractor.FindAssetsReferencedBy(target))
            {
                var asset = a;

                if (asset is MonoScript) // Skip script references
                    continue;

                if (asset is Component or GameObject)
                {
                    asset = AssetDatabase.LoadAssetAtPath<Object>(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(asset)) ?? asset;
                }

                var assetNode = TryGetNode(asset, nodes);

                var conKey = $"{node.id}->{assetNode.id}";
                if (!connections.ContainsKey(conKey))
                {
                    var con = new Connection { from = node.id, to = assetNode.id };
                    connections.Add(conKey, con);
                }

                CreateNodesAndConnections(asset, nodes, connections);
            }
        }

        private static Node TryGetNode(Object target, Dictionary<Object, Node> nodes)
        {
            if (!nodes.TryGetValue(target, out var node))
            {
                node = new Node
                {
                    name = target.name,
                };
                nodes.Add(target, node);
            }

            return node;
        }

        public static class AssetReferenceExtractor
        {
            /// <summary>
            /// Given an object, find all assets in the Project it references.
            /// </summary>
            public static List<Object> FindAssetsReferencedBy(Object target)
            {
                if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(target)))
                {
                    string folderPath = AssetDatabase.GetAssetPath(target);
                    return AssetDatabase.FindAssets("", new[] { AssetDatabase.GetAssetPath(target) })
                        .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                        .Where(path => Path.GetDirectoryName(path).Replace("\\", "/") == folderPath)
                        .Select(path => AssetDatabase.LoadAssetAtPath<Object>(path))
                        .Where(o => o != null)
                        .ToList();
                }

                var results = new List<Object>();
                if (target == null)
                    return results;

                // Support Assembly Definition Assets
                if (target is AssemblyDefinitionAsset asmdef)
                {
                    return GetAsmdefReferences(asmdef);
                }
                var so = new SerializedObject(target);
                var iterator = so.GetIterator();

                var unique = new HashSet<Object>();

                while (iterator.NextVisible(true))
                {
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        var referenced = iterator.objectReferenceValue;

                        if (referenced == null)
                            continue;

                        // Only count assets in the Project, not scene objects
                        var path = AssetDatabase.GetAssetPath(referenced);
                        if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/"))
                        {
                            if (!unique.Contains(referenced))
                            {
                                unique.Add(referenced);
                                results.Add(referenced);
                            }
                        }
                    }
                }

                return results;
            }

            private static List<Object> GetAsmdefReferences(AssemblyDefinitionAsset asmdef)
            {
                var results = new List<Object>();
                var path = AssetDatabase.GetAssetPath(asmdef);
                var json = File.ReadAllText(path);

                var data = new AsmdefData();
                EditorJsonUtility.FromJsonOverwrite(json, data);

                if (data.references == null)
                    return results;

                foreach (var referenceGUID in data.references)
                {
                    // reference is a GUID string (Unity stores them this way internally)
                    var refPath = AssetDatabase.GUIDToAssetPath(referenceGUID.Replace("GUID:", ""));

                    if (string.IsNullOrEmpty(refPath))
                        continue;

                    var refAsm = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(refPath);
                    if (refAsm != null)
                        results.Add(refAsm);
                }
                return results;
            }

            /// <summary>
            /// Helper class to match UnityAssemblyDefinitionImporter JSON structure.
            /// </summary>
            [System.Serializable]
            private class AsmdefData
            {
                public string name;
                public string[] references;
            }
        }
    }
}
#endif
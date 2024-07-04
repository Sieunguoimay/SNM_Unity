#if UNITY_EDITOR
using Common.UnityExtend.UIElements.GraphView;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public partial class AssetDependencyGraph
{
    private class NodeArrangementItem
    {
        public DependencyNode node;
        public NodeArrangementItem parent;
        public List<NodeArrangementItem> children;
        public int depth;
    }
    private class DependencyEdge : EdgeView
    {
        public bool IsAddressableReference;
        protected override Color Color => IsAddressableReference && !IsSelected ? Color.red : base.Color;
    }

    private static class DependencyGraphCreator
    {
        public static DependencyNode CreateGraph(Object rootObject,
            bool ignoreScript, bool ignoreBuiltIn,
            out List<DependencyNode> nodes,
            out List<DependencyEdge> edges,
            out IReadOnlyList<NodeArrangementItem> arrangementItems)
        {
            var rootPath = AssetDatabase.GetAssetPath(rootObject);

            edges = new List<DependencyEdge>();

            var nodeDict = new Dictionary<string, NodeArrangementItem>();
            var rootNode = TraverseToCreateGraph(nodeDict, edges, rootPath, 0, ignoreScript, ignoreBuiltIn);
            rootNode.depth = 0;
            rootNode.node.style.left = 0;
            rootNode.node.style.top = 0;
            nodes = nodeDict.Values.Select(a => a.node).ToList();

            arrangementItems = nodeDict.Values.ToArray();

            ArrangeNodes(arrangementItems, 0f, 0f);

            return rootNode.node;
        }

        public static void ArrangeNodes(IEnumerable<NodeArrangementItem> arangementItems, float startX, float startY)
        {
            var columns = arangementItems.OrderBy(i => i.depth).GroupBy(i => i.depth);

            var x = 0f;
            foreach (var column in columns)
            {
                var columnItems = column.ToArray();
                var height = (columnItems.Length - 1) * 40f;
                var parents = columnItems.Where(i => i.parent != null).Select(i => i.parent).Distinct().ToArray();
                var parent = parents.FirstOrDefault();
                var sameParent = parents.Count() <= 1;
                var y = (sameParent && parent != null ? (parent.node.style.top.value.value + parent.node.style.height.value.value / 2f) : 0f) - height / 2f;
                //Debug.Log($"=> {string.Join(",", parents.SelectMany(p => p.node.hierarchy.Children().Select(c => (c as ObjectField).value.name)))}");
                foreach (var g in columnItems)
                {
                    g.node.style.left = x;
                    g.node.style.top = y;
                    g.node.DefaultPosition = new Vector2(x, y);
                    g.node.defaultPosition = new Vector2(x, y);
                    y += 40f;
                }

                x += 200f;
            }

            foreach (var i in arangementItems)
            {
                i.node.style.left = i.node.DefaultPosition.x + startX;
                i.node.style.top = i.node.DefaultPosition.y + startY;
            }
        }

        private static NodeArrangementItem TraverseToCreateGraph(
            Dictionary<string, NodeArrangementItem> nodeDict,
            List<DependencyEdge> edgeList, string path, int depth, bool ignoreScript, bool ignoreBuiltIn)
        {
            var parentNode = new NodeArrangementItem { node = CreateNode(path), depth = int.MaxValue, children = new() };
            nodeDict.Add(path, parentNode);
            var paths = GetReferences(path, ignoreScript, ignoreBuiltIn, out var addressableStartIndex);
            for (int i = 0; i < paths.Length; i++)
            {
                string p = paths[i];
                if (p.Equals(path)) continue;
                var foundInDict = nodeDict.TryGetValue(p, out var dNode);
                if (!foundInDict)
                {
                    dNode = TraverseToCreateGraph(nodeDict, edgeList, p, depth + 1, ignoreScript, ignoreBuiltIn);
                }
                if (dNode.depth > depth + 1)
                {
                    parentNode.children.Add(dNode);
                    dNode.parent = parentNode;
                    dNode.depth = depth + 1;

                    if (foundInDict)
                    {
                        UpdateNodeDepthRecurr(dNode);
                    }
                }
                if (dNode != parentNode)
                {
                    var edge = new DependencyEdge
                    {
                        IsAddressableReference = i >= addressableStartIndex
                    };
                    edge.Connect(parentNode.node, dNode.node);
                    edgeList.Add(edge);
                }
            }

            return parentNode;
        }

        private static void UpdateNodeDepthRecurr(NodeArrangementItem item)
        {
            foreach (var c in item.children)
            {
                c.depth = item.depth + 1;
                UpdateNodeDepthRecurr(c);
            }
        }

        private static string[] GetReferences(string path, bool ignoreScript, bool ignoreBuiltIn, out int addressableStartIndex)
        {
            string fullPath = Path.Combine(Application.dataPath, path["Assets/".Length..]);

            if (File.Exists(fullPath))
            {
                string assetText = File.ReadAllText(fullPath);
                var a = GUIDToPath(ParseReferences(assetText)).ToArray();
                var b = GUIDToPath(ParseAddressableReference(assetText));
                addressableStartIndex = a.Length;
                return a.Concat(b).ToArray();
            }
            addressableStartIndex = -1;
            return new string[0];

            IEnumerable<string> GUIDToPath(IEnumerable<string> guids)
            {
                return guids.Distinct()
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => WhereCondition(p, ignoreScript, ignoreBuiltIn));
            }

            static bool WhereCondition(string p, bool ignoreSript, bool ignoreBuiltIn)
            {
                var pass = !string.IsNullOrEmpty(p) && !AssetDatabase.IsValidFolder(p);
                if (ignoreSript)
                {
                    pass &= !p.EndsWith(".cs");
                }
                if (ignoreBuiltIn)
                {
                    pass &= p.StartsWith("Assets");
                }
                return pass;// && p.Contains('.');
            }
        }
        public static IEnumerable<string> ParseReferences(string text)
        {
            var referencePattern = @"\{fileID:\s(-?\d+),\s+guid:\s+([\w\d]+)(?:,\s+type:\s+(\d+))?\}";
            string guidPattern = @"guid:\s+([\w\d]+)";

            var matches = Regex.Matches(text, referencePattern);

            foreach (Match match in matches.Cast<Match>())
            {
                var m = Regex.Match(match.Value, guidPattern);

                if (m.Success && m.Groups.Count >= 2)
                {
                    var guidGroup = m.Groups[1];
                    yield return guidGroup.Value;
                }
            }
        }
        public static IEnumerable<string> ParseReferences2(string text)
        {
            //var referencePattern = @"\{fileID:\s(-?\d+),\s+guid:\s+([\w\d]+)(?:,\s+type:\s+(\d+))?\}";
            var referencePattern = @"(\w+):\s*\{fileID:\s(-?\d+),\s+guid:\s+([\w\d]+)(?:,\s+type:\s+(\d+))?\}";
            string guidPattern = @"guid:\s+([\w\d]+)";
            string fieldNamePattern = @"^([^:]+):";

            var matches = Regex.Matches(text, referencePattern);

            foreach (Match match in matches.Cast<Match>())
            {
                var fieldName = Regex.Match(match.Value, fieldNamePattern);
                if (fieldName.Success && fieldName.Groups[1].Value.Equals("m_CorrespondingSourceObject")) continue;

                var guid = Regex.Match(match.Value, guidPattern);

                if (guid.Success && guid.Groups.Count >= 2)
                {
                    var guidGroup = guid.Groups[1];
                    yield return guidGroup.Value;
                }
            }
        }
        public static IEnumerable<(string, string)> ParseReferences3(string text)
        {
            var referencePattern = @"(\w+):\s*\{fileID:\s(-?\d+),\s+guid:\s+([\w\d]+)(?:,\s+type:\s+(\d+))?\}";
            string guidPattern = @"guid:\s+([\w\d]+)";
            string fieldNamePattern = @"^([^:]+):";

            var matches = Regex.Matches(text, referencePattern);

            foreach (Match match in matches.Cast<Match>())
            {
                var guid = Regex.Match(match.Value, guidPattern);
                var fieldName = Regex.Match(match.Value, fieldNamePattern);

                if (guid.Success && guid.Groups.Count >= 2)
                {
                    var guidGroup = guid.Groups[1];
                    yield return (fieldName.Success ? fieldName.Groups[1].Value : "", guidGroup.Value);
                }
            }
        }
        private static IEnumerable<string> ParseAddressableReference(string text)
        {
            var pattern = @"m_AssetGUID:\s*([a-fA-F0-9]{32})";
            var matches = Regex.Matches(text, pattern);

            foreach (Match m in matches.Cast<Match>())
            {

                if (m.Success && m.Groups.Count >= 2)
                {
                    yield return m.Groups[1].Value;
                }
            }
        }
        private static DependencyNode CreateNode(string path)
        {
            var node = new DependencyNode(path);
            return node;
        }
    }
}
#endif
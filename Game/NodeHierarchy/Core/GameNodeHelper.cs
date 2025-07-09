using System.Collections.Generic;
using UnityEngine;

namespace Snm.Framework.NodeHierarchy
{
    public static class GameNodeHelper
    {
        public static TNode GetNodeInParent<TNode>(this IGameNode node) where TNode : IGameNode
        {
            if (node.Parent == null) return default;
            if (node.Parent is TNode rn) return rn;
            return GetNodeInParent<TNode>(node.Parent);
        }

        public static IEnumerable<IGameNode> Iterate(this IGameNode node)
        {
            yield return node;
            foreach (var c in node.GetChildren())
            {
                foreach (var cc in Iterate(c))
                {
                    yield return cc;
                }
            }
        }
    }
}
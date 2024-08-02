using UnityEngine;

namespace GameNode
{
    public static class GameNodeHelper
    {
        public static TNode GetNodeInParent<TNode>(this IGameNode node) where TNode : IGameNode
        {
            if (node.Parent == null) return default;
            if (node.Parent is TNode rn) return rn;
            return GetNodeInParent<TNode>(node.Parent);
        }
    }
}
using UnityEngine;

namespace GameNode
{
    public static class GameNodeExtensions
    {
        public static TNode GetNodeInParent<TNode>(this IGameNode node) where TNode : IGameNode
        {
            if (node is TNode rn) return rn;
            if (node.Parent == null) return default;
            return GetNodeInParent<TNode>(node.Parent);
        }

        public static TDependency GetDependencyInSystemNode<TDependency>(this IGameNode node, string key) where TDependency : class
        {
            var systemNode = GetNodeInParent<ISystemNode>(node);
            if (systemNode == null)
            {
                Debug.LogError("GetDependencyInParent Failed! System Node not found");
                return default;
            }
            var dependency = systemNode.Dependencies.GetObject<TDependency>(key);
            if (dependency == null)
            {
                Debug.LogError($"GetDependencyInParent Failed! Dependency not found for key {key}");
                return default;
            }

            return dependency;
        }
    }
}
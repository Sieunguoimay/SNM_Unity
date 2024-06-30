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
    }
}
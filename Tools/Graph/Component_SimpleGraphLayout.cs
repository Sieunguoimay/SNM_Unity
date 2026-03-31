#if UNITY_EDITOR
using UnityEngine;

namespace Snm.Tools.GraphPresentation
{
    public interface IGraphLayout
    {
        void LayoutGraph(Graph graph, bool cyclic);
    }

    public class Component_SimpleGraphLayout : IGraphLayout
    {
        private readonly Vector2 spacing;

        public Component_SimpleGraphLayout(string config)
        {
            var spacingParts = config.Trim('(', ')').Split(',');
            spacing = new Vector2(float.Parse(spacingParts[0]), float.Parse(spacingParts[1]));
        }

        void IGraphLayout.LayoutGraph(Graph graph, bool cyclic) => GraphLayout.LayoutGraph(graph, spacing, cyclic);

        public static string SerializeConfig(Vector2 spacing)
        {
            return $"({spacing.x},{spacing.y})";
        }
    }
}
#endif
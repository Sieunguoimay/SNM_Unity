#if UNITY_EDITOR
using UnityEngine.UIElements;

namespace Snm.Tools.GraphPresentation
{
    public interface IGraphVEBuilder
    {
        VisualElement CreateGraphVE(Graph graph);
    }

    public class Component_DefaultGraphVEBuilder : IGraphVEBuilder
    {
        VisualElement IGraphVEBuilder.CreateGraphVE(Graph graph)
            => GraphVEBuilder.BuildGraphVE(graph, new DefaultNodeVEBuilder());
    }
}
#endif
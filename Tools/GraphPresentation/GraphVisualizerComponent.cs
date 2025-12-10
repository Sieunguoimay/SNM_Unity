#if UNITY_EDITOR
using System;

namespace Snm.Tools.GraphPresentation
{
    [Serializable]
    public class GraphVisualizerComponent
    {
        public string type;
        public string data;
    }

    public class GraphVisualizerComponent<TComp> : GraphVisualizerComponent
    {
        GraphVisualizerComponent(Type implType, string data)
        {
            if (!typeof(TComp).IsAssignableFrom(implType))
            {
                throw new ArgumentException($"implType must implement {typeof(TComp).Name}");
            }

            this.type = implType.AssemblyQualifiedName;
            this.data = data;
        }

        public static GraphVisualizerComponent<TComp> Create<TCompImpl>(string data)
            where TCompImpl : class, TComp
        {
            return new GraphVisualizerComponent<TComp>(typeof(TCompImpl), data);
        }
    }
}
#endif
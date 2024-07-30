#if UNITY_EDITOR
using UnityEditor;

namespace SceneViewDrawModes
{
    public interface ICustomDrawMode
    {
        public string Name { get; }
        public bool IsActive { get; }
        void Setup(SceneView sceneView);
        void TearDown();
        void Update();
    }
}

#endif

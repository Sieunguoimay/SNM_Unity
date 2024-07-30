#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SceneViewDrawModes
{
    public class CustomDrawMode_Bone : ICustomDrawMode
    {
        public string Name => "Bone";
        public bool IsActive { get; private set; }
        private SceneView _sceneView;

        public void Setup(SceneView sceneView)
        {
            _sceneView = sceneView;
            IsActive = true;
            _sceneView.SetSceneViewShaderReplace(null, null);
        }

        public void TearDown()
        {
            _sceneView.SetSceneViewShaderReplace(null, null);
            IsActive = false;
        }

        public void Update()
        {
            Debug.Log("CustomDrawMode_Bone");
        }
    }
}

#endif

using UnityEngine;

namespace GameNode
{
    public class NodeEntryPoint : MonoBehaviour
    {
        [ObjectSelector]
        [SerializeField]
        private Object rootNode;

        private IGameNode RootNode => rootNode as IGameNode;

        private void OnEnable()
        {
            RootNode.Setup();
        }

        private void OnDisable()
        {
            RootNode.TearDown();
        }
    }
}
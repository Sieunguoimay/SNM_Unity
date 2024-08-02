using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameNode
{
    public class NodeEntryPoint : MonoBehaviour
    {
        [ObjectSelector(typeof(IGameNode))]
        [SerializeField]
        private Object[] rootNodes;

        private IEnumerable<IGameNode> RootNodes => rootNodes.Select(rootNode => rootNode as IGameNode);

        private void OnEnable()
        {
            foreach (var r in RootNodes)
            {
                r.Setup();
            }
        }

        private void OnDisable()
        {
            foreach (var r in RootNodes)
            {
                r.TearDown();
            }
        }
    }
}
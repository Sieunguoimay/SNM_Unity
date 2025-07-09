using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Snm.Tools;
using UnityEngine;

namespace Snm.Framework.NodeHierarchy
{
    public class NodeEntryPoint : MonoBehaviour
    {
        [TypeSelector(typeof(IGameNode))]
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
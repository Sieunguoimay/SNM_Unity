using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GameNode
{
    public class NodeMB : MonoBehaviour, IGameNode
    {
        [SerializeField]
        private ChildNodeEntry[] children;
        private readonly GameNode gameNode = new();

        public bool IsSetup => gameNode.IsSetup;
        public IGameNode Parent => gameNode.Parent;

        public virtual void Setup()
        {
            foreach (var c in children)
            {
                c.Inject(this);
                gameNode.AddNode(c.ChildNode);
            }

            gameNode.Setup();
        }

        public virtual void TearDown()
        {
            gameNode.TearDown();

            foreach (var c in children)
            {
                gameNode.RemoveNode(c.ChildNode);
                c.Eject();
            }
        }

        public void AddNode(IGameNode node) => gameNode.AddNode(node);
        public void RemoveNode(IGameNode node) => gameNode.RemoveNode(node);
        public IEnumerable<IGameNode> GetChildren() => gameNode.GetChildren();
        public void SetParent(IGameNode node) => gameNode.SetParent(node);


#if UNITY_EDITOR
        private void SetSourceObject_Editor()
        {
            if (children != null)
            {
                foreach (var i in children)
                {
                    i.SetSource_Editor(this);
                }
            }
        }
        
        [CustomEditor(typeof(NodeMB), true)]
        private class NodeEditor : Editor
        {
            private NodeMB _target;

            void OnEnable()
            {
                _target ??= target as NodeMB;
                _target.SetSourceObject_Editor();
            }
        }
#endif
    }
}
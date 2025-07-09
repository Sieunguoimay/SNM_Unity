using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Snm.Framework.NodeHierarchy
{
    public class NodeMB : MonoBehaviour, IGameNode
    {
        [SerializeField]
        private ChildNodeEntry[] children;
        private readonly IGameNode gameNode = new GameNode();

        bool IGameNode.IsSetup => gameNode.IsSetup;
        IGameNode IGameNode.Parent => gameNode.Parent;

        event Action<IGameNode> IGameNode.SetupStatusChanged
        {
            add => gameNode.SetupStatusChanged += value; remove => gameNode.SetupStatusChanged -= value;
        }
        event Action<IGameNode> IGameNode.ChildNodeAdded
        {
            add => gameNode.ChildNodeAdded += value; remove => gameNode.ChildNodeAdded -= value;
        }
        event Action<IGameNode> IGameNode.ChildNodeRemoved
        {
            add => gameNode.ChildNodeRemoved += value; remove => gameNode.ChildNodeRemoved -= value;
        }
        event Action<IGameNode> IGameNode.ParentChanged
        {
            add => gameNode.ParentChanged += value; remove => gameNode.ParentChanged -= value;
        }

        public virtual void Setup()
        {
            gameNode.Setup();
            AddSerializedChildren();
        }

        public virtual void TearDown()
        {
            RemoveSerializedChildren();
            gameNode.TearDown();
        }

        private void AddSerializedChildren()
        {
            foreach (var c in children)
            {
                c.Inject(this);
                gameNode.AddNode(c.ChildNode);
            }
        }

        private void RemoveSerializedChildren()
        {
            foreach (var c in children)
            {
                gameNode.RemoveNode(c.ChildNode);
                c.Eject();
            }
        }

        void IGameNode.AddNode(IGameNode node) => gameNode.AddNode(node);
        void IGameNode.RemoveNode(IGameNode node) => gameNode.RemoveNode(node);
        IEnumerable<IGameNode> IGameNode.GetChildren() => gameNode.GetChildren();
        void IGameNode.SetParent(IGameNode node) => gameNode.SetParent(node);

#if UNITY_EDITOR

        [CustomEditor(typeof(NodeMB), true)]
        private class NodeEditor : Editor
        {
            private NodeMB _target;

            void OnEnable()
            {
                _target ??= target as NodeMB;
                SetPreviewSourceObject();
            }

            private void SetPreviewSourceObject()
            {
                if (_target.children != null)
                {
                    foreach (var i in _target.children)
                    {
                        i.SetPreviewSource(_target);
                    }
                }
            }
        }
#endif
    }
}
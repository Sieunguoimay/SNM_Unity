using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace GameNode
{
    public class NodeSO : ScriptableObject, IGameNode
    {
        [SerializeField]
        private NodeSO[] children;

        [PropertyGUI(nameof(OnInjectorsGUI), true)]
        [SerializeField]
        // private ChildrenDependencyInjector injector;
        private ChildrenDependencyInjector[] injectors;

        private readonly List<IGameNode> runtimeChildren = new();

        public IGameNode Parent { get; private set; } = null;

        [field: NonSerialized]
        public bool IsSetup { get; private set; } = false;

        private void OnInjectorsGUI()
        {
            foreach (var i in injectors)
            {
                i.SetParent(this);
            }
        }

        public virtual void Setup()
        {
            foreach (var child in GetChildren())
            {
                child?.Setup();
            }
            IsSetup = true;
        }

        public virtual void TearDown()
        {
            foreach (var child in GetChildren())
            {
                child?.TearDown();
            }
            IsSetup = false;
        }

        public void AddNode(IGameNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            node.Parent?.RemoveNode(node);
            runtimeChildren.Add(node);
            node.SetParent(this);

            if (IsSetup)
            {
                node.Setup();
            }
        }

        public void RemoveNode(IGameNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            runtimeChildren.Remove(node);
            node.SetParent(null);

            if (node.IsSetup)
            {
                node.TearDown();
            }
        }

        public virtual IEnumerable<IGameNode> GetChildren()
        {
            foreach (var c in children)
            {
                yield return c;
            }
            foreach (var c in runtimeChildren)
            {
                yield return c;
            }
        }

        public void SetParent(IGameNode node)
        {
            Parent = node;
        }
    }

    [Serializable]
    public class ChildrenDependencyInjector
    {
        [SerializeField] private NodeSO child;
        [SerializeField] private NodeDependencyInjector[] injectors;

        public void SetParent(NodeSO parent)
        {
            foreach (var i in injectors)
            {
                i.SetParentAndChild(parent, child);
            }
        }

        public void Inject(object parentNode)
        {
            foreach (var i in injectors)
            {
                i.Inject(parentNode);
            }
        }

        public void Eject()
        {
            foreach (var i in injectors)
            {
                i.Eject();
            }
        }
    }

    [Serializable]
    public class NodeDependencyInjector
    {
        private NodeSO _parent;
        private NodeSO _child;

        [StringSelector(nameof(ParentMembers))]
        [SerializeField] private string parentMemberName;

        [StringSelector(nameof(ChildMembers))]
        [SerializeField] private string childMemberName;

        public void SetParentAndChild(NodeSO parent, NodeSO child)
        {
            _parent = parent;
            _child = child;
        }

        public IEnumerable<string> ParentMembers => _parent == null ? Enumerable.Empty<string>() :
            _parent.GetType()
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.MemberType == MemberTypes.Property)
            .Select(m => m.Name);

        public IEnumerable<string> ChildMembers => _child == null ? Enumerable.Empty<string>() :
            _child.GetType()
            .GetMembers(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.MemberType == MemberTypes.Field)
            .Select(m => m.Name);

        public void Inject(object parentNode)
        {

        }

        public void Eject()
        {

        }
    }
}
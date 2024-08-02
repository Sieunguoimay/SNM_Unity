using System;
using System.Collections.Generic;

namespace GameNodeHierarchy
{
    public interface IGameNode
    {
        void Setup();
        void TearDown();
        bool IsSetup { get; }
        event Action<IGameNode> SetupStatusChanged;

        void AddNode(IGameNode node);
        void RemoveNode(IGameNode node);
        IEnumerable<IGameNode> GetChildren();
        event Action<IGameNode> ChildNodeAdded;
        event Action<IGameNode> ChildNodeRemoved;

        IGameNode Parent { get; }
        void SetParent(IGameNode node);
        event Action<IGameNode> ParentChanged;
    }
}
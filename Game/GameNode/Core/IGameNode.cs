using System;
using System.Collections.Generic;

namespace GameNode
{
    public interface IGameNode
    {
        void Setup();
        void TearDown();
        bool IsSetup { get; }

        void AddNode(IGameNode node);
        void RemoveNode(IGameNode node);
        IEnumerable<IGameNode> GetChildren();

        IGameNode Parent { get; }
        void SetParent(IGameNode node);
    }
}
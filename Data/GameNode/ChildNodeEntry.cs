using System;
using System.Linq;
using Reflection;
using UnityEngine;

namespace GameNode
{
    [Serializable]
    public partial class ChildNodeEntry
    {
        [ObjectSelector(typeof(IGameNode), false)]
        [SerializeField]
        private UnityEngine.Object childNode;

        [SerializeField]
        private ReflectiveFieldAssigner[] assigners;

        public IGameNode ChildNode => childNode as IGameNode;

#if UNITY_EDITOR
        public void SetSource_Editor(object source)
        {
            foreach (var i in assigners)
            {
                i.SetSourceAndDest(source, childNode);
            }
        }
#endif

        public void Inject(object parentNode)
        {
            foreach (var i in assigners)
            {
                i.SetSourceAndDest(parentNode, childNode);
                i.Assign();
            }
        }

        public void Eject()
        {
            foreach (var i in assigners)
            {
                i.Unassign();
                i.SetSourceAndDest(null, null);
            }
        }
    }

}


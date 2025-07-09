using System;
using System.Linq;
using Reflection;
using Snm.Tools;
using UnityEngine;

namespace Snm.Framework.NodeHierarchy
{
    [Serializable]
    public partial class ChildNodeEntry
    {
        [TypeSelector(typeof(IGameNode), false)]
        [SerializeField]
        private UnityEngine.Object childNode;

        [SerializeField]
        private ReflectiveFieldAssigner[] assigners;

        public IGameNode ChildNode => childNode as IGameNode;

        public void Inject(object parentNode)
        {
#if UNITY_EDITOR
            AssertUnassigned_Editor(parentNode);
#endif
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

#if UNITY_EDITOR
        public void SetPreviewSource(object source)
        {
            foreach (var i in assigners)
            {
                i.SetSourceAndDest(source, childNode);
            }
        }

        private void AssertUnassigned_Editor(object parentNode)
        {
            foreach (var u in ReflectiveFieldAssigner.GetDestMembers(childNode)
                .Where(m => !assigners.Any(a => a.DestMemberName == m)))
            {
                Debug.LogError($"Found unassigned field marked as [InjectField]: {u}", parentNode as UnityEngine.Object);
            }
        }
#endif
    }
}


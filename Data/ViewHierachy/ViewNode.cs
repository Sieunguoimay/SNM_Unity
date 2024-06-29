using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEngine;

namespace Supports.ViewHierachy
{
    public class ViewNode : MonoBehaviour
    {
        [SerializeField] private ChildViewNode[] children;

        public object DynamicData { get; private set; }
        private bool _isSetup;

        public ViewNode Self => this;
        public IEnumerable<ViewNode> Children => children.Select(c => c.viewNode);

        public event Action<ViewNode> SetupEvent;

        public virtual void Setup(object data)
        {
            DynamicData = data;

            _isSetup = true;

            SetupChildren();

            SetupEvent?.Invoke(this);
        }

        public virtual void TearDown()
        {
            TearDownChildren();

            _isSetup = false;

            DynamicData = default;
        }

        public void SetupChildrenAgain()
        {
            TearDownChildren();
            SetupChildren();
        }

        private void TearDownChildren()
        {
            foreach (var child in children)
            {
                child.TearDownChild();
            }
        }

        private void SetupChildren()
        {
            foreach (var child in children)
            {
                child.SetupChild(this);
            }
        }

        protected virtual Type GetReflectionType()
        {
            return GetType();
        }

        protected virtual object GetReflectionData()
        {
            return Self;
        }

        private static readonly Regex ExtractMemberNameRegex = new("^[a-zA-Z0-9_]+");
        private static readonly BindingFlags BindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        protected virtual IEnumerable<string> AllGetMembersOfDataView => GetMembers(GetReflectionType())
            .Select(m => $"{m.Name}{(m is MethodInfo ? "()" : "")}: {GetMemberReturnType(m)?.Name}").ToArray();

        private IEnumerable<MemberInfo> GetMembers(Type type)
        {
            if (type == null) return Enumerable.Empty<MemberInfo>();

            var members = GetMembersByHierarchy(type);
            var excludeMethodInfos = members.OfType<PropertyInfo>().SelectMany(p => p.GetAccessors()).ToArray();
            return members.Where(m => m is not MethodInfo mi || !excludeMethodInfos.Contains(mi) && mi.ReturnType != typeof(void));
        }

        private Type GetMemberReturnType(MemberInfo member)
        {
            return (member as PropertyInfo)?.PropertyType ?? (member as FieldInfo)?.FieldType ?? (member as MethodInfo)?.ReturnType;
        }

        protected virtual object GetMemberData(string memberName)
        {
            var sourceType = GetReflectionType();
            var sourceData = GetReflectionData();
            var mName = ExtractMemberNameRegex.Match(memberName).Value;
            return sourceType.GetField(mName, BindingFlags)?.GetValue(sourceData)
                    ?? sourceType.GetProperty(mName, BindingFlags)?.GetValue(sourceData)
                    ?? sourceType.GetMethod(mName, BindingFlags)?.Invoke(sourceData, new object[] { });
        }

        private static IEnumerable<MemberInfo> GetMembersByHierarchy(Type type)
        {
            var members = new List<MemberInfo>();

            members.AddRange(type.GetMembers());

            if (type.BaseType != null)
            {
                members.AddRange(GetMembersByHierarchy(type.BaseType));
            }

            return members.OrderBy(member => member.DeclaringType == type ? 0 : 1);
        }

        [Serializable]
        private class ChildViewNode
        {
            [StringSelector(nameof(AllGetMembersOfDataView), true)]
            [SerializeField] private string memberName;

            [ObjectSelector]
            [SerializeField] public ViewNode viewNode;

            private ViewNode _parent;

            public void SetupChild(ViewNode parent)
            {
                _parent = parent;

                var childViewNodeData = _parent.GetMemberData(memberName);

                if (childViewNodeData != null && Validate(childViewNodeData))
                {
                    if (!viewNode._isSetup)
                    {
                        viewNode.Setup(childViewNodeData);
                    }
                    else
                    {
                        Debug.LogError($"Child ViewNode already set up! {viewNode.name} -> {viewNode.DynamicData.GetType().Name}", _parent);
                    }
                }
            }

            public void TearDownChild()
            {
                if (viewNode._isSetup)
                {
                    viewNode.TearDown();
                }
                else
                {
                    //if (logError)
                    //{
                    //    Debug.LogError($"Child ViewNode had not been set up! {viewNode.name}", _parent);
                    //}
                }
                _parent = null;
            }

            public bool Validate(object selectedData)
            {
                var genericArg = GetGenericArgument(viewNode.GetType(), typeof(ViewNode<>));
                if (genericArg != null)
                {
                    if (!genericArg.IsAssignableFrom(selectedData.GetType()))
                    {
                        Debug.LogError($"ChildViewNodeDataSelector setup failed! Type of givenData ({selectedData.GetType().Name}) does not match Typeof viewNode ({genericArg.Name})!", _parent);
                        return false;
                    }
                }
                return true;
            }

            private static Type GetGenericArgument(Type derivedType, Type genericBaseType)
            {
                var currentType = derivedType;

                while (currentType != null)
                {
                    var baseType = currentType.BaseType;

                    if (baseType != null && baseType.IsGenericType && baseType.GetGenericTypeDefinition() == genericBaseType)
                    {
                        return baseType.GetGenericArguments()[0];
                    }

                    currentType = baseType;
                }

                return null;
            }
        }
    }
}


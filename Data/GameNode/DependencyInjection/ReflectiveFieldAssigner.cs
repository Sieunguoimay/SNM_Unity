using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Reflection
{
    [Serializable]
    public class ReflectiveFieldAssigner
    {

#if UNITY_EDITOR
        [StringSelector(nameof(SrcMembers), false, false, false)]
#endif
        [SerializeField]
        private string srcMemberName;

#if UNITY_EDITOR
        [StringSelector(nameof(DestMembers), false, false, false)]
#endif
        [SerializeField]
        private string destMemberName;

        private object _source;
        private object _destination;
        private MemberInfo _destMemberInfo;

        private static BindingFlags SourceFlags => BindingFlags.Public | BindingFlags.Instance;
        private static BindingFlags DestinationFlags => BindingFlags.NonPublic | BindingFlags.Instance;

#if UNITY_EDITOR
        public IEnumerable<string> SrcMembers => _source == null ? Enumerable.Empty<string>() :
            _source.GetType()
            .GetMembers(SourceFlags)
            .Where(m => m.MemberType == MemberTypes.Property)
            .Select(m => m.Name);

        public IEnumerable<string> DestMembers => GetDestMembers(_destination);

        public static IEnumerable<string> GetDestMembers(object destination)
        {
            return destination == null ? Enumerable.Empty<string>() :
            destination.GetType()
            .GetMembers(DestinationFlags)
            .Where(m => m.MemberType == MemberTypes.Field && m.GetCustomAttribute<InjectFieldAttribute>() != null)
            .Select(m => m.Name);
        }
#endif

        public void SetSourceAndDest(object source, object destination)
        {
            _source = source;
            _destination = destination;
        }

        public void Assign()
        {
            if (TryGetMemberInfo(_source.GetType(), srcMemberName, SourceFlags, out var mi))
            {
                var value = GetMemberValue(mi, _source);
                if (TryGetMemberInfo(_destination.GetType(), destMemberName, DestinationFlags, out var cMi))
                {
                    SetMemberValue(cMi, _destination, value);
                    _destMemberInfo = cMi;
                }
                else
                {
                    Debug.LogError($"Failed to Inject. Member {destMemberName} not Found", _source as UnityEngine.Object);
                }
            }
            else
            {
                Debug.LogError($"Failed to Inject. Member {srcMemberName} not Found", _source as UnityEngine.Object);
            }
        }

        public void Unassign()
        {
            SetMemberValue(_destMemberInfo, _destination, null);
        }

        public static object GetMemberValue(MemberInfo mi, object source)
        {
            return mi switch
            {
                PropertyInfo pi => pi.GetValue(source),
                FieldInfo fi => fi.GetValue(source),
                MethodInfo methodInfo => methodInfo.Invoke(source, null),
                _ => null,
            };
        }

        public static void SetMemberValue(MemberInfo mi, object target, object value)
        {
            switch (mi)
            {
                case PropertyInfo pi:
                    pi.SetValue(target, value);
                    break;
                case FieldInfo fi:
                    fi.SetValue(target, value);
                    break;
                case MethodInfo methodInfo:
                    methodInfo.Invoke(target, new[] { value });
                    break;
                default:
                    break;
            }
        }

        public static bool TryGetMemberInfo(Type type, string memberName, BindingFlags flags, out MemberInfo memberInfo)
        {
            var t = type;
            while (t != null)
            {
                var mi = t.GetMember(memberName, flags);
                if (mi != null && mi.Length > 0)
                {
                    memberInfo = mi[0];
                    return true;
                }
                else
                {
                    t = t.BaseType;
                }
            }

            memberInfo = null;
            return false;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(ReflectiveFieldAssigner))]
        private class ReflectiveFieldAssignerDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                // base.OnGUI(position, property, label);
                var r1 = new Rect(position.x, position.y, position.width / 2 - 12, position.height);
                EditorGUI.PropertyField(r1, property.FindPropertyRelative(nameof(destMemberName)));

                var r2 = new Rect(position.x + position.width / 2 - 12, position.y, 24, position.height);
                EditorGUI.LabelField(r2, " <- ");

                var r3 = new Rect(position.x + position.width / 2 + 12, position.y, position.width / 2 - 12, position.height);
                EditorGUI.PropertyField(r3, property.FindPropertyRelative(nameof(srcMemberName)));
            }
        }
#endif
    }
}


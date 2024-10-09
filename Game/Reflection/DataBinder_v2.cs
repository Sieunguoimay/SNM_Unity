using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using InspectorExtensions;
using UnityEngine;

namespace Reflection
{
    public class DataBinder_v2 : MonoBehaviour
    {
        [SerializeField] private bool bindOnEnable;
        [ObjectSelector]
        [SerializeField] private UnityEngine.Object sourceObject;
        [SerializeField] private MemberPairConfig[] memberPairConfigs;

        private MemberPair[] _memberPairs;

#if UNITY_EDITOR
        [RevealNonSerialized]
        public bool IsReady => _memberPairs != null;
        [RevealNonSerialized]
        public string ConnectedEvents => _memberPairs != null ? string.Join(", ", _memberPairs.SelectMany(p => p.Events.Select(e => e.EventInfo.Name))) : "";
#endif

        private void OnEnable()
        {
            CreateMemberPairs();

            if (bindOnEnable)
            {
                BindAll();
            }
        }

        private void OnDisable()
        {
            ClearMemberPairs();
        }

        private void CreateMemberPairs()
        {
            _memberPairs = new MemberPair[memberPairConfigs.Length];
            for (int i = 0; i < memberPairConfigs.Length; i++)
            {
                var mc = memberPairConfigs[i];
                var m = new MemberPair();
                m.getter.Setup(sourceObject, mc.sourceMember);
                m.setter.Setup(mc.destObject, mc.destMember);
                m.SetupEvents();
                _memberPairs[i] = m;
            }
        }

        private void ClearMemberPairs()
        {
            foreach (var m in _memberPairs)
            {
                m.TearDownEvents();
                m.getter.TearDown();
                m.setter.TearDown();
            }
            _memberPairs = null;
        }

        [ContextMenu("BindAll")]
        public void BindAll()
        {
            if (_memberPairs == null) return;
            foreach (var m in _memberPairs)
            {
                m.Bind();
            }
        }

        public static BindingFlags Flag => BindingFlags.Public | BindingFlags.Instance;
        public IEnumerable<string> AllSourceMembers => GetAllGetterMembers(sourceObject).Distinct();

        public static IEnumerable<string> GetAllGetterMembers(object obj)
        {
            if (obj != null)
            {
                yield return "@SourceObject";

                var t = obj.GetType();

                while (t != null)
                {
                    foreach (var m in t.GetProperties(Flag))
                    {
                        if (m.GetGetMethod() != null)
                        {
                            yield return $"{m.Name}: {m.PropertyType.Name}";
                        }
                    }

                    foreach (var m in t.GetMethods(Flag))
                    {
                        if (m.ReturnType == typeof(void)) continue;
                        var parameters = m.GetParameters();
                        if (parameters.Length != 0) continue;
                        if (m.Name.StartsWith("get_")) continue;

                        yield return $"{m.Name}(): {m.ReturnType.Name}";
                    }

                    foreach (var m in t.GetFields(Flag))
                    {
                        yield return $"{m.Name}: {m.FieldType.Name}";
                    }

                    t = t.BaseType;
                }
            }
        }

        public static MemberInfo GetGetterMemberInfo(object obj, string memberName)
        {
            if (obj != null)
            {
                var t = obj.GetType();

                while (t != null)
                {
                    foreach (var m in t.GetProperties(Flag))
                    {
                        if (m.GetGetMethod() != null)
                        {
                            if (memberName == $"{m.Name}: {m.PropertyType.Name}") return m;
                        }
                    }

                    foreach (var m in t.GetMethods(Flag))
                    {
                        if (m.ReturnType == typeof(void)) continue;
                        var parameters = m.GetParameters();
                        if (parameters.Length != 0) continue;
                        if (m.Name.StartsWith("get_")) continue;

                        if (memberName == $"{m.Name}(): {m.ReturnType.Name}") return m;
                    }

                    foreach (var m in t.GetFields(Flag))
                    {
                        if (memberName == $"{m.Name}: {m.FieldType.Name}") return m;
                    }

                    t = t.BaseType;
                }
            }
            return null;
        }

        public static IEnumerable<string> GetAllSetterMembers(object obj)
        {
            if (obj != null)
            {
                var t = obj.GetType();

                while (t != null)
                {
                    foreach (var m in t.GetMethods(Flag))
                    {
                        var parameters = m.GetParameters();
                        if (parameters.Length != 1) continue;
                        if (m.Name.StartsWith("set_")) continue;

                        yield return $"{m.Name} ({parameters[0].ParameterType.Name})";
                    }

                    foreach (var m in t.GetProperties(Flag))
                    {
                        if (m.GetSetMethod() != null)
                        {
                            yield return $"{m.Name}: {m.PropertyType.Name}";
                        }
                    }

                    foreach (var m in t.GetFields(Flag))
                    {
                        yield return $"{m.Name}: {m.FieldType.Name}";
                    }

                    t = t.BaseType;
                }
            }
        }

        public static MemberInfo GetSetterMemberInfo(object obj, string memberName)
        {
            if (obj != null)
            {
                var t = obj.GetType();

                while (t != null)
                {
                    foreach (var m in t.GetMethods(Flag))
                    {
                        var parameters = m.GetParameters();
                        if (parameters.Length != 1) continue;
                        if (m.Name.StartsWith("set_")) continue;

                        if (memberName == $"{m.Name} ({parameters[0].ParameterType.Name})") return m;
                    }

                    foreach (var m in t.GetProperties(Flag))
                    {
                        if (m.GetSetMethod() != null)
                        {
                            if (memberName == $"{m.Name}: {m.PropertyType.Name}") return m;
                        }
                    }

                    foreach (var m in t.GetFields(Flag))
                    {
                        if (memberName == $"{m.Name}: {m.FieldType.Name}") return m;
                    }

                    t = t.BaseType;
                }
            }
            return null;
        }

        public static IEnumerable<EventInfo> GetAllEventInfos(object obj)
        {
            if (obj != null)
            {
                var t = obj.GetType();

                while (t != null)
                {
                    foreach (var m in t.GetEvents(Flag))
                    {
                        yield return m;
                    }

                    t = t.BaseType;
                }
            }
        }

        [Serializable]
        private class MemberPairConfig
        {
            [StringSelector(nameof(AllSourceMembers), true)]
            public string sourceMember;

            [ObjectSelector]
            public UnityEngine.Object destObject;

            [StringSelector(nameof(AllDestMembers))]
            public string destMember;

            public IEnumerable<string> AllDestMembers => GetAllSetterMembers(destObject).Distinct();
        }

        private class MemberPair
        {
            public readonly Getter getter = new();
            public readonly Setter setter = new();

            private ReflectiveEvent[] _events;

            public ReflectiveEvent[] Events => _events;

            public void SetupEvents()
            {
                if (getter.MemberInfo == null) return;
                var atts = getter.MemberInfo.GetCustomAttributes<PropertyChangeEventAttribute>();
                var eventInfos = GetAllEventInfos(getter.Object).Distinct().ToArray();
                var events = new List<ReflectiveEvent>();
                foreach (var att in atts)
                {
                    if (!string.IsNullOrEmpty(att.EventName))
                    {
                        var ev = new ReflectiveEvent();
                        var eventInfo = eventInfos.FirstOrDefault(ei => ei.Name == att.EventName);
                        if (eventInfo != null)
                        {
                            ev.Setup(getter.Object, eventInfo);
                            ev.OnEventInvoked += OnEventInvoked;
                            events.Add(ev);
                        }
                        else
                        {
                            UnityEngine.Debug.LogError($" Event {att.EventName} is not found");
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"ChangeEventAttribute of {getter.MemberInfo.Name}. EventName is not assigned");
                    }
                }
                _events = events.ToArray();
            }

            public void TearDownEvents()
            {
                if (_events == null) return;
                foreach (var ev in _events)
                {
                    ev.OnEventInvoked -= OnEventInvoked;
                    ev.TearDown();
                }
                _events = null;
            }

            private void OnEventInvoked(ReflectiveEvent @event)
            {
                Bind();
            }

            public void Bind()
            {
                setter.SetData(getter.GetData());
            }
        }

        private class ReflectiveItem
        {
            protected object _obj;
            protected MemberInfo _memberInfo;

            public MemberInfo MemberInfo => _memberInfo;
            public object Object => _obj;

            public virtual void Setup(object obj, string memberName) { }

            public virtual void TearDown()
            {
                _obj = null;
                _memberInfo = null;
            }
        }

        private class Getter : ReflectiveItem
        {
            private bool _isGettingSourceObject;

            public override void Setup(object obj, string memberName)
            {
                _obj = obj;
                if (memberName == "@SourceObject")
                {
                    _isGettingSourceObject = true;
                }
                else
                {
                    _isGettingSourceObject = false;
                    _memberInfo = GetGetterMemberInfo(obj, memberName);
                }
            }

            public object GetData()
            {
                if (_isGettingSourceObject) return _obj;

                if (_memberInfo is PropertyInfo propertyInfo)
                {
                    return propertyInfo.GetValue(_obj);
                }
                else if (_memberInfo is FieldInfo fieldInfo)
                {
                    return fieldInfo.GetValue(_obj);
                }
                else if (_memberInfo is MethodInfo methodInfo)
                {
                    return methodInfo.Invoke(_obj, null);
                }
                return null;
            }
        }

        private class Setter : ReflectiveItem
        {
            public override void Setup(object obj, string memberName)
            {
                _obj = obj;
                _memberInfo = GetSetterMemberInfo(obj, memberName);
            }

            public void SetData(object data)
            {
                if (_memberInfo is PropertyInfo propertyInfo)
                {
                    propertyInfo.SetValue(_obj, data);
                }
                else if (_memberInfo is FieldInfo fieldInfo)
                {
                    fieldInfo.SetValue(_obj, data);
                }
                else if (_memberInfo is MethodInfo methodInfo)
                {
                    methodInfo.Invoke(_obj, new[] { data });
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class PropertyChangeEventAttribute : System.Attribute
    {
        public string EventName { get; }

        public PropertyChangeEventAttribute(string eventName)
        {
            EventName = eventName;
        }
    }

    public class ReflectiveEvent
    {
        private object _obj;
        private EventInfo _eventInfo;
        private Delegate _eventHandler;

        public EventInfo EventInfo => _eventInfo;

        public event Action<ReflectiveEvent> OnEventInvoked;

        public void Setup(object obj, EventInfo eventInfo)
        {
            _obj = obj;
            _eventInfo = eventInfo;
            _eventHandler = CreateEventHandler();
            _eventInfo.AddEventHandler(obj, _eventHandler);
        }

        public void TearDown()
        {
            _eventInfo.RemoveEventHandler(_obj, _eventHandler);
        }

        private Delegate CreateEventHandler()
        {
            var eventHandlerParameterTypes = _eventInfo.EventHandlerType.GetMethod("Invoke").GetParameters()
                .Select(p => p.ParameterType).ToArray();

            var parameters = eventHandlerParameterTypes.Select(t => Expression.Parameter(t)).ToArray();

            var constant = Expression.Constant(this);
            var method = GetType().GetMethod(nameof(OnSourceEventInvoked), BindingFlags.NonPublic | BindingFlags.Instance);
            var call = Expression.Call(constant, method);
            var lambda = Expression.Lambda(call, parameters);

            return lambda.Compile();
        }

        private void OnSourceEventInvoked()
        {
            OnEventInvoked?.Invoke(this);
        }
    }
}
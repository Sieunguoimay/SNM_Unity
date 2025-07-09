using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Snm.Tools;
using UnityEngine;

namespace Reflection
{
    /*
    todo: Update DataBinder to support ChangeEventAttribute of a SourceMember. 
    This attribute associates the SourceMember with a selected event. So that,
    we don't have to serialize the event, instead we hardcode it. And since the 
    member change is always triggered by one or more events.
    */
    
    [Obsolete]
    public class DataBinder : MonoBehaviour
    {
        [SerializeField] private bool bindOnEnable;
        [TypeSelector]
        [SerializeField] private UnityEngine.Object sourceObject;
        [SerializeField] private ReflectionEvent[] events;
        [SerializeField] private ReflectionConnect[] connections;

        private Type SourceType => sourceObject?.GetType();
        private static BindingFlags BindingFlags => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
        private IEnumerable<string> SourceEvents => SourceType?.GetEvents(BindingFlags)?.Select(e => e.Name);
        private IEnumerable<string> SourceGetMembers
        {
            get
            {
                if (SourceType == null) return Enumerable.Empty<string>();

                return GetAllTypes(SourceType)
                    .SelectMany(i => i.GetMembers(BindingFlags))
                    .Where(m => m is PropertyInfo || m is FieldInfo)
                    .Select(e => $"{e.Name}: {(e as PropertyInfo)?.PropertyType?.Name ?? (e as FieldInfo)?.FieldType?.Name}")
                    .Distinct();
            }
        }

        private static readonly Regex ExtractMemberNameRegex = new("^[a-zA-Z0-9_.]+");

        private void OnEnable()
        {
            Setup();

            if (bindOnEnable)
            {
                Bind();
            }
        }

        private void OnDisable()
        {
            TearDown();
        }

        [ContextMenu("Setup")]
        private void Setup()
        {
            foreach (var e in events)
            {
                e.Setup(sourceObject);
                e.InvokeEvent -= OnEventInvoked;
                e.InvokeEvent += OnEventInvoked;
            }
            foreach (var c in connections)
            {
                c.Setup(sourceObject);
            }
        }

        [ContextMenu("TearDown")]
        private void TearDown()
        {
            foreach (var e in events)
            {
                e.TearDown();
                e.InvokeEvent -= OnEventInvoked;
            }
            foreach (var g in connections)
            {
                g.TearDown();
            }
        }

        private void OnEventInvoked(ReflectionEvent obj)
        {
            Bind();
        }

        [ContextMenu("Bind")]
        public void Bind()
        {
            foreach (var g in connections)
            {
                g.BindData();
            }
        }

        private static IEnumerable<Type> GetAllTypes(Type type)
        {
            var t = type;
            yield return t;
            foreach (var i in t.GetInterfaces())
            {
                yield return i;
            }
            while (t.BaseType != null)
            {
                t = t.BaseType;
                yield return t;
            }
        }

        private static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            foreach (var t in GetAllTypes(type))
            {
                var f = t.GetField(fieldName, BindingFlags);
                if (f != null) return f;
            }
            return null;
        }

        private static PropertyInfo GetPropertyInfo(Type type, string propName)
        {
            foreach (var t in GetAllTypes(type))
            {
                var f = t.GetProperty(propName, BindingFlags);
                if (f != null) return f;
            }
            return null;
        }

        private static MethodInfo GetMethodInfo(Type type, string methName)
        {
            foreach (var t in GetAllTypes(type))
            {
                var f = t.GetMethod(methName, BindingFlags);
                if (f != null) return f;
            }
            return null;
        }

        [Serializable]
        private class ReflectionEvent
        {
            [StringSelector(nameof(SourceEvents), true)]
            public string sourceEventName;

            private object _target;

            private EventInfo _eventInfo;
            private Delegate _eventHandler;

            public event Action<ReflectionEvent> InvokeEvent;

            public void Setup(object target)
            {
                _target = target;
                _eventInfo = target.GetType().GetEvent(sourceEventName, BindingFlags);
                _eventHandler = CreateEventHandler();
                _eventInfo.AddEventHandler(target, _eventHandler);
            }

            public void TearDown()
            {
                _eventInfo.RemoveEventHandler(_target, _eventHandler);
            }

            private Delegate CreateEventHandler()
            {
                var eventHandlerParameterTypes = _eventInfo.EventHandlerType.GetMethod("Invoke").GetParameters()
                    .Select(p => p.ParameterType).ToArray();

                var parameters = eventHandlerParameterTypes.Select(t => Expression.Parameter(t)).ToArray();

                var lambdaExpression = Expression.Lambda(
                    Expression.Call(Expression.Constant(this), GetType().GetMethod(nameof(OnSourceEventInvoked), BindingFlags)),
                    parameters
                );

                return lambdaExpression.Compile();
            }

            private void OnSourceEventInvoked()
            {
                InvokeEvent?.Invoke(this);
            }
        }

        [Serializable]
        private class ReflectionConnect
        {
            [StringSelector(nameof(SourceGetMembers), true)]
            public string sourceMemberName;

            [TypeSelector]
            public UnityEngine.Object targetObject;

            [StringSelector(nameof(TargetTypeSetMembers))]
            public string targetMemberName;

            private IEnumerable<string> TargetTypeSetMembers => GetAllSetMethodsAndSetProperties();
            private BindingFlags Flag => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            private FieldInfo _sourceFieldInfo;
            private PropertyInfo _sourcePropertyInfo;
            private MethodInfo _targetMethodInfo;
            private PropertyInfo _targetPropertyInfo;

            private object _source;

            private IEnumerable<string> GetAllSetMethodsAndSetProperties()
            {
                if (targetObject != null)
                {
                    var typed = targetObject.GetType();
                    foreach (var type in GetAllTypes(typed))
                    {
                        var properties = type.GetProperties(Flag);
                        var propertyAutoMethods = properties.SelectMany(p => p.GetAccessors()).ToArray();

                        foreach (var p in properties)
                        {
                            if (p.GetSetMethod() != null)
                            {
                                yield return $"{p.Name}: {p.PropertyType.Name}";
                            }
                        }

                        foreach (var m in type.GetMethods(Flag))
                        {
                            if (!propertyAutoMethods.Contains(m))
                            {
                                if (m.GetParameters().Count() == 1)
                                {
                                    yield return $"{m.Name}({string.Join(',', m.GetParameters().Select(p => p.ParameterType.Name))})";
                                }
                            }
                        }
                    }
                }
            }

            public void Setup(object source)
            {
                _source = source;

                var sourceType = _source.GetType();
                var targetType = targetObject.GetType();

                var sourceMember = ExtractMemberNameRegex.Match(sourceMemberName).Value;
                var targetMember = ExtractMemberNameRegex.Match(targetMemberName).Value;

                _sourceFieldInfo = GetFieldInfo(sourceType, sourceMember);
                _sourcePropertyInfo = GetPropertyInfo(sourceType, sourceMember);
                _targetMethodInfo = GetMethodInfo(targetType, targetMember);
                _targetPropertyInfo = GetPropertyInfo(targetType, targetMember);
            }

            public void TearDown()
            {
                _sourceFieldInfo = null;
                _sourcePropertyInfo = null;
                _targetMethodInfo = null;
                _targetPropertyInfo = null;
            }

            public void BindData()
            {
                var data = _sourceFieldInfo?.GetValue(_source) ?? _sourcePropertyInfo?.GetValue(_source);
                if (_targetMethodInfo != null)
                {
                    _targetMethodInfo.Invoke(targetObject, new[] { data });
                }
                else
                {
                    _targetPropertyInfo?.SetValue(targetObject, data);
                }
            }

            public bool IsValidConnection()
            {
                var dataType = _sourceFieldInfo?.FieldType ?? _sourcePropertyInfo?.PropertyType;
                var desiredType = _targetMethodInfo.GetParameters().FirstOrDefault().ParameterType;
                return desiredType.IsAssignableFrom(dataType);
            }
        }
    }
}
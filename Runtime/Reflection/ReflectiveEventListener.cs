using System.Reflection;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine.Events;
using Snm.Tools;

namespace Reflection
{
    public class ReflectiveEventListener : MonoBehaviour
    {
        [TypeSelector]
        [SerializeField] private UnityEngine.Object target;
        [SerializeField] private ReflectionEvent[] events;
        [SerializeField] private UnityEvent onTriggered;

        private Type SourceType => target?.GetType();
        private static BindingFlags BindingFlags => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private IEnumerable<string> SourceEvents => SourceType?.GetEvents(BindingFlags)?.Select(e => e.Name);

        private void OnEnable()
        {
            foreach (var e in events)
            {
                e.OnTrigger = onTriggered;
                e.Setup(target);
            }
        }

        private void OnDisable()
        {
            foreach (var e in events)
            {
                e.TearDown();
            }
        }

        [Serializable]
        private class ReflectionEvent
        {
            [StringSelector(nameof(SourceEvents), true)]
            public string sourceEventName;

            private object _target;

            private EventInfo _eventInfo;
            private Delegate _eventHandler;

            [field: System.NonSerialized]
            public UnityEvent OnTrigger { get; set; }

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
                OnTrigger?.Invoke();
            }
        }
    }
}

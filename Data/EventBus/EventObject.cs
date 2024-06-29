using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EventBus
{
    public interface IEventObject
    {
        Type ConstraintDataType { get; }
        string EventName { get; }

        public static bool TryCastData<TData>(object data, out TData outputData)
        {
            if (data is TData d)
            {
                outputData = d;
                return true;
            }
            outputData = default;
            return false;
        }
    }

    public class EventObject : ScriptableObject, IEventObject
    {
        [Tooltip("Empty means no constraint")]
        [StringSelector(nameof(ConstraintTypes))]
        [SerializeField] private string constraintDataType;

        private Type _constraintDataType;
        public string EventName => name;
        public IEventObject Interface => this;
        public Type ConstraintDataType
        {
            get
            {
                if (Application.isPlaying)
                {
                    if (_constraintDataType == null)
                    {
                        _constraintDataType = Type.GetType(constraintDataType);
                    }
                    return _constraintDataType;
                }
                else
                {
                    return Type.GetType(constraintDataType);
                }
            }
        }

        public IEnumerable<string> ConstraintTypes => AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().Select(t => t.AssemblyQualifiedName));
    }

}
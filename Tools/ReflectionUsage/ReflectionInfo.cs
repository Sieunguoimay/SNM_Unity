#if UNITY_EDITOR
using System;
using UnityEngine;

namespace ReflectionUsage
{
    [Serializable]
    public class ReflectionInfo
    {
        [SerializeField] private string typeStr;
        private Type _type;
        public Type Type
        {
            get
            {
                if (_type == null)
                {
                    _type = Type.GetType(typeStr);
                }
                return _type;
            }
            set
            {
                _type = value;
                typeStr = _type.AssemblyQualifiedName;
            }
        }
        public string member;
    }
}
#endif
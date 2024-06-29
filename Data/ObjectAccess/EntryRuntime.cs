using System;

namespace ObjectAccess
{
    public class ObjectEntryRuntime
    {
        public Type Type { get; }
        public object BindedObject { get; set;}
        public bool IsRegistered => BindedObject != null;
        public ObjectEntryRuntime(Type type)
        {
            Type = type;
        }
    }

}
using System;

namespace Reflection
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class PropertyChangeEventAttribute : System.Attribute
    {
        public string EventName { get; }

        public PropertyChangeEventAttribute(string eventName)
        {
            EventName = eventName;
        }
    }
}
using System;

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

}
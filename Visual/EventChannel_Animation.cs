using System;
using EventBus;

public class EventChannel_Animation : EventChannelMB, IEventSender
{
    public void TriggerStringEvent(string parameter)
    {
        Dispatch(AnimationEventObject, this, parameter);
    }

    public static IEventObject AnimationEventObject { get; } = new AnimationEventObject();

}
public class AnimationEventObject : IEventObject
{
    public Type ConstraintDataType => typeof(string);

    public string EventName => "animation_event_object";
}


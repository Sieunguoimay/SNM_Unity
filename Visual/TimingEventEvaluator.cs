using UnityEngine;
using UnityEngine.Events;

public class TimingEventEvaluator : MonoBehaviour
{
    public UnityEvent onTrigger;
    public float triggerThreshold = 0f;

    private bool _hasTriggered = false;

    public void Evaluate(float value)
    {
        if (value > triggerThreshold)
        {
            if (!_hasTriggered)
            {
                onTrigger?.Invoke();
                _hasTriggered = true;
            }
        }
        else
        {
            if (_hasTriggered)
            {
                ResetTrigger();
            }
        }
    }

    public void ResetTrigger()
    {
        _hasTriggered = false;
    }

}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class IntervalEvent : MonoBehaviour
{
    [SerializeField] private float intervalSecs = 1f;
    [SerializeField] private UnityEvent onTrigger;

    public event Action<IntervalEvent> TriggerEvent;
    
    private void Start()
    {
        StartCoroutine(Interval());
    }

    private IEnumerator Interval()
    {
        while (true)
        {
            yield return new WaitForSeconds(intervalSecs);
            TriggerEvent?.Invoke(this);
            onTrigger?.Invoke();
        }
    }
}
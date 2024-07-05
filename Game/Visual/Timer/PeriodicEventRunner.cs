using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PeriodicEventRunner : MonoBehaviour
{
    [SerializeField] private float periodSecs = 1f;
    [SerializeField] private bool triggerOnStart = false;
    [SerializeField] private UnityEvent onTrigger;

    public event Action<PeriodicEventRunner> TriggerEvent;

    private void Start()
    {
        StartCoroutine(IE_IntervalLoop());
    }

    private IEnumerator IE_IntervalLoop()
    {
        if (triggerOnStart)
        {
            TriggerEvent?.Invoke(this);
            onTrigger?.Invoke();
        }

        while (true)
        {
            yield return new WaitForSeconds(periodSecs);
            TriggerEvent?.Invoke(this);
            onTrigger?.Invoke();
        }
    }
}
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Snm.Components.Timers
{
    public class PeriodicEventRunner : MonoBehaviour
    {
        [SerializeField] private float periodSecs = 1f;
        [SerializeField] private bool triggerOnStart = false;
        [FormerlySerializedAs("runOnStart")]
        [SerializeField] private bool runOnEnable = true;
        [SerializeField] private UnityEvent onTrigger;

        public float PeriodSecs { get; private set; }

        public event Action<PeriodicEventRunner> TriggerEvent;

        private void OnEnable()
        {
            if (runOnEnable)
            {
                StartRunning();
            }
        }

        private void OnDisable()
        {
            StopRunning();
        }

        public void StartRunning()
        {
            SetPeriodSecs(periodSecs);
            StartCoroutine(IE_IntervalLoop());
        }

        public void StopRunning()
        {
            StopAllCoroutines();
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
                yield return new WaitForSeconds(PeriodSecs);
                TriggerEvent?.Invoke(this);
                onTrigger?.Invoke();
            }
        }

        public void SetPeriodSecs(float periodSecs)
        {
            PeriodSecs = periodSecs;
        }
    }
}
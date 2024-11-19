using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForceAnim
{
    public class PhysicsForceAnimation : MonoBehaviour, IForceAnimation
    {
        [SerializeField] private Rigidbody target;
        [SerializeField] private ForceEvaluator[] forces;
        [SerializeField] private float simulationDuration = 10f;
        [SerializeField] private bool clearForcesOnPlayEnd = true;

        public Vector3 Velocity => target.linearVelocity;
        public float Time => _time;
        public float SimulationDuration => simulationDuration;
        public IReadOnlyList<ForceEvaluator> Forces => forces;
        public Transform Target => target.transform;

        private float _time = 0f;
        private Coroutine _playCoroutine;
        private Vector3 _startPosition;

        private void Start()
        {
            _startPosition = target.transform.position;
        }

        [ContextMenu("Play")]
        public void Play()
        {
            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
            }
            _playCoroutine = StartCoroutine(Loop());
        }

        private IEnumerator Loop()
        {
            _startPosition = target.transform.position;
            _time = 0f;
            while (true)
            {
                var totalForce = Vector3.zero;
                foreach (var f in forces)
                {
                    if (f.enabled)
                    {
                        totalForce += f.DoEvaluate(this);
                    }
                }
                ApplyForce(totalForce, UnityEngine.Time.deltaTime);
                _time += UnityEngine.Time.deltaTime;

                if (_time > simulationDuration)
                {
                    break;
                }

                yield return null;
            }

            if (clearForcesOnPlayEnd)
            {
                ClearAllForces();
            }
        }

        private void ApplyForce(Vector3 totalForce, float deltaTime)
        {
            target.AddForce(totalForce * deltaTime);
        }

        [ContextMenu("ClearAllForces")]
        private void ClearAllForces()
        {
            target.linearVelocity = Vector3.zero;
            target.angularVelocity = Vector3.zero;
            target.Sleep();
        }

        [ContextMenu("ResetPosition")]
        private void ResetPosition()
        {
            target.transform.position = _startPosition;
        }
    }
}
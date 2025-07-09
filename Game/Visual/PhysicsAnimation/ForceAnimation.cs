using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Visual.PhysicsAnimation
{
    public interface IForceAnimation
    {
        Transform Target { get; }
        Vector3 Velocity { get; }
        float Time { get; }
        float SimulationDuration { get; }
        IReadOnlyList<ForceEvaluator> Forces { get; }
        void Play();
    }
    
    public class ForceAnimation : MonoBehaviour, IForceAnimation
    {
        [SerializeField] private Transform target;
        [SerializeField] private float mass = 1f;
        [SerializeField] private float startSpeed = 0f;
        [SerializeField] private Vector3 startSpeedDirection = Vector3.forward;
        [SerializeField] private ForceEvaluator[] forces;
        [SerializeField] private float simulationDuration = 10f;

        public Transform Target => target;
        public Vector3 Velocity => _velocity;
        public float Time => _time;
        public float SimulationDuration => simulationDuration;
        public IReadOnlyList<ForceEvaluator> Forces => forces;

        private Vector3 _velocity = Vector3.zero;
        private float _time = 0f;
        private Vector3 _startPosition;

        private void Start()
        {
            _startPosition = target.position;
        }

        public void Play()
        {
            ResetStartValues();

            StartCoroutine(Loop());
        }

        [ContextMenu("TestStart")]
        private void TestStart()
        {
            StopAllCoroutines();
            Play();
        }

        private void ResetStartValues()
        {
            target.position = _startPosition;
            _velocity = startSpeedDirection * startSpeed;
            _time = 0f;
        }

        private IEnumerator Loop()
        {
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
                    ResetStartValues();
                    break;
                }

                yield return null;
            }
        }

        public void ApplyForce(Vector3 force, float deltaTime)
        {
            var acceleration = force / mass;
            _velocity += acceleration * deltaTime;
            target.position += _velocity * deltaTime;
        }
    }
}
using System.Collections;
using UnityEngine;

public class AttractionEffectAnim : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float acc = 15f;
    
    public void FlyUpToTarget(Transform flyer, Transform target)
    {
        StartCoroutine(IEFlyUpToTarget(flyer, target));
    }

    private IEnumerator IEFlyUpToTarget(Transform flyer, Transform target)
    {
        var offset = target.position - flyer.position;
        var sqrDistance = offset.sqrMagnitude;
        var stopDistance = 1f;

        var velocity = .4f * speed * Random.insideUnitSphere;

        while (sqrDistance > stopDistance)
        {
            offset = target.position - flyer.position;

            var dir = offset.normalized;

            var vel = speed * dir;

            var acceleration = Vector3.ClampMagnitude(vel - velocity, acc);

            velocity += acceleration * Time.deltaTime;

            flyer.position += Time.deltaTime * velocity;

            sqrDistance = offset.sqrMagnitude;

            yield return null;
        }

        Destroy(flyer.gameObject);
    }
}
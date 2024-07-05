using System.Collections;
using UnityEngine;

public class FXSpawner : MonoBehaviour
{
    private static FXSpawner _instance;
    public static FXSpawner Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameObject("FXSpawner").AddComponent<FXSpawner>();
            }
            return _instance;
        }
    }

    public void SpawnFlying(GameObject prefab, Transform target)
    {
        var copy = Instantiate(prefab, transform);
        copy.transform.position = prefab.transform.position;
        StartCoroutine(FlyUpToTarget(copy.transform, target));
    }

    private IEnumerator FlyUpToTarget(Transform flyer, Transform target)
    {
        var speed = 20f;
        var acc = 15f;
        var offset = target.position - flyer.position;
        var sqrDistance = offset.sqrMagnitude;
        var stopDistance = 1f;

        var velocity = Random.insideUnitSphere * speed * .4f;

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
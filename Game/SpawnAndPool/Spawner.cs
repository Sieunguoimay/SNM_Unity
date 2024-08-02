using System.Collections.Generic;
using UnityEngine;

namespace GameNodeHierarchy
{
    public class Spawner : MonoBehaviour
    {
        [SerializeField] private Transform container;

        public IReadOnlyList<GameObject> SpawnedObjects { get; }

        public GameObject Spawn(GameObject prefab)
        {
            return SpawningPool.Instance.Spawn(prefab, container);
        }

        public void Despawn(GameObject spawnedObject)
        {
            SpawningPool.Instance.Despawn(spawnedObject);
        }
    }
}
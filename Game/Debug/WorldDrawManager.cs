using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Snm.Debugging
{
    public class WorldDrawManager : MonoBehaviour
    {
        private static WorldDrawManager _instance;
        private static WorldDrawManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[WorldLineManager]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<WorldDrawManager>();
                    _instance.EnsureContainer();
                }
                return _instance;
            }
        }

        private readonly Queue<LineRenderer> _pool = new Queue<LineRenderer>();
        private GameObject _container;
        private Material _lineMaterial;

        private void EnsureContainer()
        {
            if (_container != null) return;

            _container = new GameObject("[DebugLineContainer]");
            _container.transform.SetParent(transform);

            // Simple, always-available shader
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));

            var preload = 40;
            for (int i = 0; i < preload; i++)
                Instance._pool.Enqueue(Instance.CreateNewRenderer());
        }

        private LineRenderer CreateNewRenderer()
        {
            var obj = new GameObject("[DebugLine]");
            obj.transform.SetParent(_container.transform);

            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.enabled = false;
            lr.material = _lineMaterial;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            return lr;
        }


        private LineRenderer Get()
            => _pool.Count > 0 ? _pool.Dequeue() : CreateNewRenderer();


        //=====================================================================
        //  MULTI-POINT LINE
        //=====================================================================
        public static void CreateLine(Vector3[] pts, Color color, float duration, float width)
        {
            if (pts == null || pts.Length < 2) return;

            var lr = Instance.Get();

            lr.gameObject.SetActive(true);
            lr.enabled = true;
            lr.positionCount = pts.Length;
            lr.SetPositions(pts);
            lr.startColor = lr.endColor = color;
            lr.startWidth = lr.endWidth = width;

            Instance.StartCoroutine(Instance.ReturnAfter(lr, duration));
        }


        //=====================================================================
        //  ZERO-ALLOC 2-POINT LINE
        //=====================================================================
        public static void CreateLine(Vector3 a, Vector3 b, Color color, float duration, float width)
        {
            LineRenderer lr = Instance.Get();

            lr.gameObject.SetActive(true);
            lr.enabled = true;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.startColor = lr.endColor = color;
            lr.startWidth = lr.endWidth = width;

            Instance.StartCoroutine(Instance.ReturnAfter(lr, duration));
        }


        //=====================================================================
        //  Debug.DrawLine replacement
        //=====================================================================
        public static void DrawLine(Vector3 a, Vector3 b, Color color,
                                    float width = 0.02f, float duration = 0f)
        {
            CreateLine(a, b, color, duration, width); // zero alloc!
        }

        //=====================================================================
        //  Return object to pool
        //=====================================================================
        private IEnumerator ReturnAfter(LineRenderer lr, float duration)
        {
            yield return new WaitForSeconds(duration > 0 ? duration : Time.deltaTime);

            lr.enabled = false;
            lr.gameObject.SetActive(false);
            _pool.Enqueue(lr);
        }
    }

}

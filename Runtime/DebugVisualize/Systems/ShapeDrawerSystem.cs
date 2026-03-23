using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_DEBUG || DEVELOPMENT_BUILD
namespace Snm.Runtime.DebugVisualize
{
    public class DebugShapeEntry : IDisposable
    {
        private LineRenderer _lineRenderer;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private GameObject _gameObject;
        private float _duration;
        private float _elapsed;
        private Action<DebugShapeEntry> _onReturn;

        public LineRenderer LineRenderer => _lineRenderer;
        public MeshRenderer MeshRenderer => _meshRenderer;
        public GameObject GameObject => _gameObject;
        public bool IsExpired => _duration > 0 && _elapsed >= _duration;

        public MeshFilter MeshFilter => _meshFilter;



        public void Setup(GameObject gameObject, LineRenderer lineRenderer, float duration, Action<DebugShapeEntry> onReturn)
        {
            _gameObject = gameObject;
            _lineRenderer = lineRenderer;
            _meshFilter = null;
            _meshRenderer = null;
            _duration = duration;
            _elapsed = 0;
            _onReturn = onReturn;
        }

        public void Setup(GameObject gameObject, MeshFilter meshFilter, MeshRenderer meshRenderer, float duration, Action<DebugShapeEntry> onReturn)
        {
            _gameObject = gameObject;
            _lineRenderer = null;
            _meshFilter = meshFilter;
            _meshRenderer = meshRenderer;
            _duration = duration;
            _elapsed = 0;
            _onReturn = onReturn;
        }

        public void Update()
        {
            _elapsed += Time.deltaTime;
        }

        public void Dispose()
        {
            _lineRenderer = null;
            _meshFilter = null;
            _meshRenderer = null;
            _gameObject = null;
            _onReturn?.Invoke(this);
        }
    }

    public class ShapeDrawerSystem : IDisposable
    {
        private readonly DebugVisualizeSettings _settings;
        private readonly LineRendererPool _linePool;
        private readonly Queue<DebugShapeEntry> _activeLines = new();
        private readonly Queue<DebugShapeEntry> _activeMeshes = new();
        private readonly List<DebugShapeEntry> _toRemove = new();
        private Material _defaultMaterial;
        private GameObject _container;

        public ShapeDrawerSystem(DebugVisualizeSettings settings)
        {
            _settings = settings;
            _container = new GameObject("DebugShapeContainer");
            _container.hideFlags = HideFlags.HideAndDontSave;

            CreateDefaultMaterial();
            _linePool = new LineRendererPool(_defaultMaterial, settings.LineRendererPoolSize);
            _linePool.Prewarm(settings.LineRendererPoolSize);
        }

        private void CreateDefaultMaterial()
        {
            _defaultMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            _defaultMaterial.hideFlags = HideFlags.HideAndDontSave;
            _defaultMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _defaultMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _defaultMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _defaultMaterial.SetInt("_ZWrite", 0);
        }

        private DebugShapeEntry CreateMeshEntry()
        {
            var go = new GameObject("DebugMesh");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(_container.transform, false);

            var meshFilter = go.AddComponent<MeshFilter>();
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.material = _defaultMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            go.SetActive(false);

            var e = new DebugShapeEntry();
            e.Setup(go, meshFilter, meshRenderer, 0, ReturnMesh);
            return e;
        }

        public void Line(Vector3 start, Vector3 end, Color? color = null, float width = 0, float duration = 0)
        {
            var lr = _linePool.Get();
            if (lr == null) return;

            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            var c = color ?? _settings.LineColor;
            lr.startColor = c;
            lr.endColor = c;

            var w = width > 0 ? width : _settings.DefaultLineWidth;
            lr.startWidth = w;
            lr.endWidth = w;

            var d = duration > 0 ? duration : _settings.DefaultDuration;

            var entry = new DebugShapeEntry();
            entry.Setup(lr.gameObject, lr, d, ReturnLine);
            _activeLines.Enqueue(entry);
        }

        public void Ray(Vector3 origin, Vector3 direction, Color? color = null, float width = 0, float duration = 0)
        {
            Line(origin, origin + direction, color, width, duration);
        }

        public void Arrow(Vector3 origin, Vector3 direction, Color? color = null, float width = 0, float duration = 0, float headLength = 0.2f, float headWidth = 0.1f)
        {
            var end = origin + direction;
            Line(origin, end, color, width, duration);

            var normalized = direction.normalized;
            var perpendicular = Vector3.Cross(normalized, Vector3.up).normalized;
            if (perpendicular.sqrMagnitude < 0.01f)
            {
                perpendicular = Vector3.Cross(normalized, Vector3.right).normalized;
            }

            var headBack = end - normalized * headLength;
            Line(headBack + perpendicular * headWidth, end, color, width, duration);
            Line(headBack - perpendicular * headWidth, end, color, width, duration);
        }

        public void Sphere(Vector3 center, float radius, Color? color = null, float duration = 0)
        {
            var entry = CreateMeshEntry();
            var mesh = CreateSphereMesh(radius);
            entry.MeshFilter.mesh = mesh;
            entry.MeshRenderer.material.color = color ?? _settings.SphereColor;
            entry.GameObject.transform.position = center;
            entry.GameObject.SetActive(true);

            var d = duration > 0 ? duration : _settings.DefaultDuration;
            _activeMeshes.Enqueue(entry);
        }

        public void Box(Vector3 center, Vector3 size, Color? color = null, float duration = 0)
        {
            var entry = CreateMeshEntry();
            var mesh = CreateBoxMesh(size);
            entry.MeshFilter.mesh = mesh;
            entry.MeshRenderer.material.color = color ?? _settings.BoxColor;
            entry.GameObject.transform.position = center;
            entry.GameObject.SetActive(true);

            var d = duration > 0 ? duration : _settings.DefaultDuration;
            _activeMeshes.Enqueue(entry);
        }

        public void Box(Bounds bounds, Color? color = null, float duration = 0)
        {
            Box(bounds.center, bounds.size, color, duration);
        }

        public void Circle(Vector3 center, Vector3 normal, float radius, Color? color = null, int segments = 32, float duration = 0)
        {
            var entry = CreateMeshEntry();
            var mesh = CreateCircleMesh(radius, segments);
            entry.MeshFilter.mesh = mesh;
            entry.MeshRenderer.material.color = color ?? _settings.CircleColor;
            entry.GameObject.transform.position = center;
            entry.GameObject.transform.rotation = Quaternion.LookRotation(normal);
            entry.GameObject.SetActive(true);

            var d = duration > 0 ? duration : _settings.DefaultDuration;
            _activeMeshes.Enqueue(entry);
        }

        public void Frustum(Camera cam, Color? color = null, float duration = 0)
        {
            if (cam == null) return;

            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            var c = color ?? _settings.BoxColor;

            DrawFrustumPlanes(cam, planes, c, duration);
        }

        private void DrawFrustumPlanes(Camera cam, Plane[] planes, Color color, float duration)
        {
            var cameraPos = cam.transform.position;
            var farCorners = new Vector3[4];
            var nearCorners = new Vector3[4];

            cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, farCorners);
            cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, nearCorners);

            for (int i = 0; i < 4; i++)
            {
                var a = cam.transform.TransformPoint(nearCorners[i]);
                var b = cam.transform.TransformPoint(nearCorners[(i + 1) % 4]);
                Line(a, b, color, 0, duration);
            }

            for (int i = 0; i < 4; i++)
            {
                var a = cam.transform.TransformPoint(farCorners[i]);
                var b = cam.transform.TransformPoint(farCorners[(i + 1) % 4]);
                Line(a, b, color, 0, duration);
            }

            for (int i = 0; i < 4; i++)
            {
                var a = cam.transform.TransformPoint(nearCorners[i]);
                var b = cam.transform.TransformPoint(farCorners[i]);
                Line(a, b, color, 0, duration);
            }
        }

        public void Cone(Vector3 origin, Vector3 direction, float angle, float length, Color? color = null, int segments = 16, float duration = 0)
        {
            var entry = CreateMeshEntry();
            var mesh = CreateConeMesh(angle, length, segments);
            entry.MeshFilter.mesh = mesh;
            entry.MeshRenderer.material.color = color ?? _settings.SphereColor;
            entry.GameObject.transform.position = origin;
            entry.GameObject.transform.rotation = Quaternion.LookRotation(direction);
            entry.GameObject.SetActive(true);

            var d = duration > 0 ? duration : _settings.DefaultDuration;
            _activeMeshes.Enqueue(entry);
        }

        private Mesh CreateSphereMesh(float radius)
        {
            var mesh = new Mesh();
            mesh.name = "DebugSphere";
            var sphere = CreateDefaultSphere();
            mesh.vertices = Array.ConvertAll(sphere.vertices, v => v * radius);
            mesh.triangles = sphere.triangles;
            mesh.normals = Array.ConvertAll(sphere.normals, n => n);
            mesh.uv = sphere.uv;
            return mesh;
        }

        private Mesh CreateBoxMesh(Vector3 size)
        {
            var mesh = new Mesh();
            mesh.name = "DebugBox";
            var box = CreateDefaultCube();
            mesh.vertices = Array.ConvertAll(box.vertices, v => Vector3.Scale(v, size * 0.5f));
            mesh.triangles = box.triangles;
            mesh.normals = Array.ConvertAll(box.normals, n => n);
            mesh.uv = box.uv;
            return mesh;
        }

        private Mesh CreateDefaultSphere()
        {
            var mesh = new Mesh();
            mesh.name = "DefaultSphere";
            int segments = 16;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            
            for (int lat = 0; lat <= segments; lat++)
            {
                float theta = lat * Mathf.PI / segments;
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);
                
                for (int lon = 0; lon <= segments; lon++)
                {
                    float phi = lon * 2 * Mathf.PI / segments;
                    float x = Mathf.Cos(phi) * sinTheta;
                    float y = cosTheta;
                    float z = Mathf.Sin(phi) * sinTheta;
                    vertices.Add(new Vector3(x, y, z));
                }
            }
            
            for (int lat = 0; lat < segments; lat++)
            {
                for (int lon = 0; lon < segments; lon++)
                {
                    int first = (lat * (segments + 1)) + lon;
                    int second = first + segments + 1;
                    triangles.AddRange(new[] { first, second, first + 1 });
                    triangles.AddRange(new[] { second, second + 1, first + 1 });
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }

        private Mesh CreateDefaultCube()
        {
            var mesh = new Mesh();
            mesh.name = "DefaultCube";
            mesh.vertices = new Vector3[]
            {
                new(-0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f),
                new(-0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f)
            };
            mesh.triangles = new int[]
            {
                0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7, 0, 1, 5, 0, 5, 4,
                2, 3, 7, 2, 7, 6, 0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5
            };
            mesh.RecalculateNormals();
            return mesh;
        }

        private Mesh CreateCircleMesh(float radius, int segments)
        {
            var mesh = new Mesh();
            mesh.name = "DebugCircle";

            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            var normals = new Vector3[segments + 1];
            var uv = new Vector2[segments + 1];

            for (int i = 0; i < segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                vertices[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                normals[i] = Vector3.forward;
                uv[i] = new Vector2(vertices[i].x / radius * 0.5f + 0.5f, vertices[i].y / radius * 0.5f + 0.5f);

                if (i < segments - 1)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i;
                    triangles[i * 3 + 2] = i + 1;
                }
            }

            vertices[segments] = Vector3.zero;
            normals[segments] = Vector3.forward;
            uv[segments] = new Vector2(0.5f, 0.5f);

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.uv = uv;

            return mesh;
        }

        private Mesh CreateConeMesh(float angle, float length, int segments)
        {
            var mesh = new Mesh();
            mesh.name = "DebugCone";

            var radius = Mathf.Tan(angle * 0.5f) * length;
            var vertices = new Vector3[segments + 2];
            var triangles = new int[segments * 3];
            var normals = new Vector3[segments + 2];

            vertices[0] = Vector3.zero;
            normals[0] = Vector3.forward;

            for (int i = 0; i < segments; i++)
            {
                var t = (float)i / (segments - 1);
                var ringAngle = t * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(ringAngle) * radius, Mathf.Sin(ringAngle) * radius, length);
                normals[i + 1] = Vector3.forward;
            }

            vertices[segments + 1] = new Vector3(0, 0, length);
            normals[segments + 1] = Vector3.forward;

            for (int i = 0; i < segments - 1; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;

            return mesh;
        }

        private void ReturnLine(DebugShapeEntry entry)
        {
            if (entry.LineRenderer != null)
            {
                _linePool.Return(entry.LineRenderer);
            }
            entry.Dispose();
        }

        private void ReturnMesh(DebugShapeEntry entry)
        {
            if (entry.MeshFilter != null)
            {
                entry.MeshFilter.mesh = null;
            }
            if (entry.GameObject != null)
            {
                entry.GameObject.SetActive(false);
            }
            _activeMeshes.Enqueue(entry);
        }

        public void Update()
        {
            _toRemove.Clear();

            foreach (var entry in _activeLines)
            {
                entry.Update();
                if (entry.IsExpired)
                {
                    _toRemove.Add(entry);
                }
            }

            foreach (var entry in _toRemove)
            {
                _activeLines.Dequeue();
                ReturnLine(entry);
            }

            _toRemove.Clear();

            foreach (var entry in _activeMeshes)
            {
                entry.Update();
                if (entry.IsExpired)
                {
                    _toRemove.Add(entry);
                }
            }

            foreach (var entry in _toRemove)
            {
                _activeMeshes.Dequeue();
                ReturnMesh(entry);
            }
        }

        public void Clear()
        {
            foreach (var entry in _activeLines)
            {
                ReturnLine(entry);
            }
            _activeLines.Clear();

            foreach (var entry in _activeMeshes)
            {
                ReturnMesh(entry);
            }
            _activeMeshes.Clear();
        }

        public void Dispose()
        {
            Clear();
            _linePool.ReturnAll();
            if (_container != null)
            {
                UnityEngine.Object.Destroy(_container);
                _container = null;
            }
            if (_defaultMaterial != null)
            {
                UnityEngine.Object.Destroy(_defaultMaterial);
                _defaultMaterial = null;
            }
        }
    }
}
#endif

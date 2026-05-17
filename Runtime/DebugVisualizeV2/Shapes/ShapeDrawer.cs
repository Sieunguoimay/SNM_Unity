using System;
using System.Collections.Generic;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.Runtime.DebugDraw
{
    // Internal — all public API goes through DebugDraw.cs
    internal sealed class ShapeDrawer : IDisposable
    {
        private readonly DebugDrawConfig     _cfg;
        private readonly GameObject          _root;
        private readonly Material            _mat;
        private readonly Queue<LineRenderer> _linePool = new();
        private readonly Queue<MeshSlot>     _meshPool = new();

        // Tracks every mesh slot ever created so Dispose can also destroy the
        // per-slot materials of currently-leased (active) slots, not just pooled ones.
        private readonly List<MeshSlot>      _allMeshSlots = new();

        // Unit meshes — each shape is scaled via transform at draw time
        private readonly Mesh _sphere;
        private readonly Mesh _box;
        private readonly Mesh _circle;
        private readonly Mesh _cone;
        private readonly Mesh _ring;

        // ── Init ─────────────────────────────────────────────────────────────

        internal ShapeDrawer(DebugDrawConfig cfg, Transform parent)
        {
            _cfg  = cfg;
            _root = new GameObject("[DebugDraw] Shapes") {  };
            _root.transform.SetParent(parent, false);

            _mat = new Material(Shader.Find("Hidden/Internal-Colored")) {  };
            _mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _mat.SetInt("_Cull",     (int)UnityEngine.Rendering.CullMode.Off);
            _mat.SetInt("_ZWrite", 0);

            for (int i = 0; i < cfg.linePoolSize; i++) _linePool.Enqueue(MakeLine());
            for (int i = 0; i < cfg.meshPoolSize; i++) _meshPool.Enqueue(MakeMeshSlot());

            _sphere = BuildSphere(16);
            _box    = BuildBox();
            _circle = BuildCircle(32);
            _cone   = BuildCone(16);
            _ring   = BuildRing(32);
        }

        // ── Draw ─────────────────────────────────────────────────────────────

        internal DrawHandle Line(Vector3 start, Vector3 end, Color? color = null, float width = 0)
        {
            var lr = TakeLine();
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            Apply(lr, color ?? _cfg.lineColor, width);
            return DrawHandle.ForLine(lr, () => ReturnLine(lr));
        }

        internal DrawHandle Arrow(Vector3 origin, Vector3 direction, Color? color = null, float width = 0,
            float headLength = 0.2f, float headWidth = 0.1f)
        {
            var lr = TakeLine();
            lr.positionCount = 5;
            WriteArrow(lr, origin, direction, headLength, headWidth);
            Apply(lr, color ?? _cfg.arrowColor, width);
            return DrawHandle.ForLine(lr, () => ReturnLine(lr));
        }

        internal DrawHandle Sphere(Vector3 center, float radius, Color? color = null)
        {
            var slot = TakeMesh(_sphere, color ?? _cfg.sphereColor);
            slot.Go.transform.SetPositionAndRotation(center, Quaternion.identity);
            slot.Go.transform.localScale = Vector3.one * radius;
            return DrawHandle.ForMesh(slot.Renderer, () => ReturnMesh(slot));
        }

        internal DrawHandle Box(Vector3 center, Vector3 size, Color? color = null)
        {
            var slot = TakeMesh(_box, color ?? _cfg.boxColor);
            slot.Go.transform.SetPositionAndRotation(center, Quaternion.identity);
            slot.Go.transform.localScale = size;
            return DrawHandle.ForMesh(slot.Renderer, () => ReturnMesh(slot));
        }

        internal DrawHandle Circle(Vector3 center, Vector3 normal, float radius, Color? color = null)
        {
            var slot = TakeMesh(_circle, color ?? _cfg.circleColor);
            slot.Go.transform.SetPositionAndRotation(center, Quaternion.LookRotation(normal));
            slot.Go.transform.localScale = Vector3.one * radius;
            return DrawHandle.ForMesh(slot.Renderer, () => ReturnMesh(slot));
        }

        internal DrawHandle Ring(Vector3 center, Vector3 normal, float radius,
            Color? color = null, float thickness = 0)
        {
            var slot = TakeMesh(_ring, color ?? _cfg.circleColor);
            var w    = thickness > 0 ? thickness : _cfg.lineWidth;
            slot.Go.transform.SetPositionAndRotation(center, Quaternion.LookRotation(normal));
            slot.Go.transform.localScale = new Vector3(radius, radius, w);
            return DrawHandle.ForMesh(slot.Renderer, () => ReturnMesh(slot));
        }

        internal DrawHandle Cone(Vector3 origin, Vector3 direction, float angleDeg, float length,
            Color? color = null)
        {
            var slot   = TakeMesh(_cone, color ?? _cfg.sphereColor);
            var radius = Mathf.Tan(angleDeg * 0.5f * Mathf.Deg2Rad) * length;
            slot.Go.transform.position   = origin;
            slot.Go.transform.rotation   = Quaternion.LookRotation(direction);
            slot.Go.transform.localScale = new Vector3(radius, radius, length);
            return DrawHandle.ForMesh(slot.Renderer, () => ReturnMesh(slot));
        }

        internal DrawHandle[] Frustum(Camera cam, Color? color = null)
        {
            if (cam == null) return null;
            var c    = color ?? _cfg.boxColor;
            var near = new Vector3[4];
            var far  = new Vector3[4];
            cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, near);
            cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane,  Camera.MonoOrStereoscopicEye.Mono, far);

            var h = new DrawHandle[12];
            for (int i = 0; i < 4; i++)
            {
                h[i]     = Line(cam.transform.TransformPoint(near[i]), cam.transform.TransformPoint(near[(i + 1) % 4]), c);
                h[i + 4] = Line(cam.transform.TransformPoint(far[i]),  cam.transform.TransformPoint(far[(i + 1) % 4]),  c);
                h[i + 8] = Line(cam.transform.TransformPoint(near[i]), cam.transform.TransformPoint(far[i]),             c);
            }
            return h;
        }

        // ── Arrow geometry ────────────────────────────────────────────────────

        private static void WriteArrow(LineRenderer lr, Vector3 origin, Vector3 direction,
            float headLength, float headWidth)
        {
            var end      = origin + direction;
            var norm     = direction.normalized;
            var perp     = Vector3.Cross(norm, Vector3.up);
            if (perp.sqrMagnitude < 0.01f) perp = Vector3.Cross(norm, Vector3.right);
            perp.Normalize();
            var headBase = end - norm * headLength;

            lr.SetPosition(0, origin);
            lr.SetPosition(1, end);
            lr.SetPosition(2, headBase + perp * headWidth);
            lr.SetPosition(3, end);
            lr.SetPosition(4, headBase - perp * headWidth);
        }

        // ── Pool ─────────────────────────────────────────────────────────────

        private LineRenderer TakeLine()
        {
            var lr = _linePool.Count > 0 ? _linePool.Dequeue() : MakeLine();
            lr.gameObject.SetActive(true);
            return lr;
        }

        private void ReturnLine(LineRenderer lr)
        {
            lr.positionCount = 0;
            lr.gameObject.SetActive(false);
            _linePool.Enqueue(lr);
        }

        private MeshSlot TakeMesh(Mesh mesh, Color color)
        {
            var slot = _meshPool.Count > 0 ? _meshPool.Dequeue() : MakeMeshSlot();
            slot.Filter.mesh             = mesh;
            slot.Renderer.material.color = color;
            slot.Go.SetActive(true);
            return slot;
        }

        private void ReturnMesh(MeshSlot slot)
        {
            if (slot.Filter != null)
                slot.Filter.mesh = null;
            if (slot.Go != null)
                slot.Go.SetActive(false);
            _meshPool.Enqueue(slot);
        }

        private void Apply(LineRenderer lr, Color color, float width)
        {
            lr.startColor = lr.endColor = color;
            lr.startWidth = lr.endWidth = width > 0 ? width : _cfg.lineWidth;
        }

        // ── Factory ───────────────────────────────────────────────────────────

        private LineRenderer MakeLine()
        {
            var go = new GameObject {  };
            go.transform.SetParent(_root.transform, false);
            go.SetActive(false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace     = true;
            lr.sharedMaterial    = _mat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;
            lr.positionCount     = 0;
            return lr;
        }

        private MeshSlot MakeMeshSlot()
        {
            var go = new GameObject {  };
            go.transform.SetParent(_root.transform, false);
            go.SetActive(false);
            var f = go.AddComponent<MeshFilter>();
            var r = go.AddComponent<MeshRenderer>();
            r.material          = new Material(_mat) {  };
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows    = false;
            var slot = new MeshSlot(go, f, r);
            _allMeshSlots.Add(slot);
            return slot;
        }

        // ── Unit mesh builders ────────────────────────────────────────────────

        private static Mesh BuildSphere(int segs)
        {
            var verts = new List<Vector3>();
            var tris  = new List<int>();
            for (int lat = 0; lat <= segs; lat++)
            {
                float theta = lat * Mathf.PI / segs;
                float st = Mathf.Sin(theta), ct = Mathf.Cos(theta);
                for (int lon = 0; lon <= segs; lon++)
                {
                    float phi = lon * 2 * Mathf.PI / segs;
                    verts.Add(new Vector3(Mathf.Cos(phi) * st, ct, Mathf.Sin(phi) * st));
                }
            }
            for (int lat = 0; lat < segs; lat++)
            for (int lon = 0; lon < segs; lon++)
            {
                int a = lat * (segs + 1) + lon, b = a + segs + 1;
                tris.AddRange(new[] { a, b, a + 1, b, b + 1, a + 1 });
            }
            var m = new Mesh { name = "DbgSphere" };
            m.SetVertices(verts); m.SetTriangles(tris, 0); m.RecalculateNormals();
            return m;
        }

        private static Mesh BuildBox()
        {
            const float s = 0.5f;
            var m = new Mesh
            {
                name      = "DbgBox",
                vertices  = new[] { new Vector3(-s,-s,-s), new Vector3(s,-s,-s), new Vector3(s,s,-s), new Vector3(-s,s,-s), new Vector3(-s,-s,s), new Vector3(s,-s,s), new Vector3(s,s,s), new Vector3(-s,s,s) },
                triangles = new[] { 0,2,1, 0,3,2, 4,5,6, 4,6,7, 0,1,5, 0,5,4, 2,3,7, 2,7,6, 0,4,7, 0,7,3, 1,2,6, 1,6,5 }
            };
            m.RecalculateNormals();
            return m;
        }

        private static Mesh BuildCircle(int segs)
        {
            var verts = new Vector3[segs + 1];
            var norms = new Vector3[segs + 1];
            var tris  = new int[segs * 3];
            for (int i = 0; i < segs; i++)
            {
                float a  = i * Mathf.PI * 2f / segs;
                verts[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                norms[i] = Vector3.forward;
            }
            verts[segs] = Vector3.zero; norms[segs] = Vector3.forward;
            for (int i = 0; i < segs; i++) { tris[i*3] = segs; tris[i*3+1] = i; tris[i*3+2] = (i+1) % segs; }
            return new Mesh { name = "DbgCircle", vertices = verts, normals = norms, triangles = tris };
        }

        private static Mesh BuildCone(int segs)
        {
            var verts = new Vector3[segs + 1];
            var norms = new Vector3[segs + 1];
            var tris  = new int[segs * 3];
            verts[0] = Vector3.zero; norms[0] = Vector3.back;
            for (int i = 0; i < segs; i++)
            {
                float a      = (float)i / segs * Mathf.PI * 2f;
                verts[i + 1] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 1f);
                norms[i + 1] = Vector3.forward;
            }
            for (int i = 0; i < segs; i++) { tris[i*3] = 0; tris[i*3+1] = i+1; tris[i*3+2] = (i+1) % segs + 1; }
            return new Mesh { name = "DbgCone", vertices = verts, normals = norms, triangles = tris };
        }

        private static Mesh BuildRing(int segs)
        {
            // Inner 0.8 / outer 1.0 — radius and thickness driven by localScale
            var verts = new Vector3[segs * 2];
            var norms = new Vector3[segs * 2];
            var tris  = new int[segs * 6];
            for (int i = 0; i < segs; i++)
            {
                float a = i * Mathf.PI * 2f / segs, c = Mathf.Cos(a), s = Mathf.Sin(a);
                verts[i*2]   = new Vector3(c * 0.8f, s * 0.8f, 0);
                verts[i*2+1] = new Vector3(c,         s,         0);
                norms[i*2] = norms[i*2+1] = Vector3.forward;
                int cur = i * 2, nxt = ((i + 1) % segs) * 2;
                tris[i*6]   = cur;     tris[i*6+1] = nxt;     tris[i*6+2] = cur + 1;
                tris[i*6+3] = nxt;     tris[i*6+4] = nxt + 1; tris[i*6+5] = cur + 1;
            }
            return new Mesh { name = "DbgRing", vertices = verts, normals = norms, triangles = tris };
        }

        internal void SetVisible(bool visible) => _root.SetActive(visible);

        // ── Cleanup ───────────────────────────────────────────────────────────

        public void Dispose()
        {
            // Destroy per-slot instance materials for EVERY slot ever created —
            // both pooled and currently-leased (active) — otherwise outstanding
            // DrawHandles' materials leak on system shutdown.
            // Use sharedMaterial to avoid creating a new instance via .material.
            foreach (var slot in _allMeshSlots)
            {
                if (slot.Renderer)
                {
                    var mat = slot.Renderer.sharedMaterial;
                    slot.Renderer.sharedMaterial = null;
                    if (mat) UnityEngineUtility.DestroyObject(mat);
                }
            }
            _allMeshSlots.Clear();
            _meshPool.Clear();

            if (_root) UnityEngineUtility.DestroyObject(_root);
            if (_mat)  UnityEngineUtility.DestroyObject(_mat);
            foreach (var mesh in new[] { _sphere, _box, _circle, _cone, _ring })
                if (mesh) UnityEngineUtility.DestroyObject(mesh);
        }

        // ── Nested ───────────────────────────────────────────────────────────

        private sealed class MeshSlot
        {
            public readonly GameObject   Go;
            public readonly MeshFilter   Filter;
            public readonly MeshRenderer Renderer;
            public MeshSlot(GameObject go, MeshFilter f, MeshRenderer r) { Go = go; Filter = f; Renderer = r; }
        }
    }
}

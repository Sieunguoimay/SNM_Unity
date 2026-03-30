#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Graphics3D.Modeling
{
    public static class MeshBooleanCSG
    {
        public enum Operation { Union, Subtract, Intersect }

        const float Epsilon = 1e-5f;

        enum Side { Coplanar = 0, Front = 1, Back = 2, Spanning = 3 }

        struct Polygon
        {
            public List<Vector3> Vertices;
            public Vector3 Normal;

            public Polygon(List<Vector3> verts)
            {
                Vertices = verts;
                Normal = MeshGeometryUtils.ComputeFaceNormal(verts[0], verts[1], verts[2]);
            }

            public Polygon Flipped()
            {
                var flipped = new List<Vector3>(Vertices);
                flipped.Reverse();
                return new Polygon(flipped);
            }

            public Plane GetPlane()
            {
                return new Plane(Normal, Vertices[0]);
            }
        }

        class BSPNode
        {
            public Plane Plane;
            public List<Polygon> Polygons = new();
            public BSPNode Front;
            public BSPNode Back;

            public BSPNode() { }

            public BSPNode(List<Polygon> polygons)
            {
                Build(polygons);
            }

            public void Build(List<Polygon> polygons)
            {
                if (polygons.Count == 0) return;

                if (Polygons.Count == 0)
                    Plane = polygons[0].GetPlane();

                var front = new List<Polygon>();
                var back = new List<Polygon>();

                foreach (var poly in polygons)
                {
                    SplitPolygon(Plane, poly, Polygons, Polygons, front, back);
                }

                if (front.Count > 0)
                {
                    if (Front == null) Front = new BSPNode();
                    Front.Build(front);
                }

                if (back.Count > 0)
                {
                    if (Back == null) Back = new BSPNode();
                    Back.Build(back);
                }
            }

            public List<Polygon> AllPolygons()
            {
                var result = new List<Polygon>(Polygons);
                if (Front != null) result.AddRange(Front.AllPolygons());
                if (Back != null) result.AddRange(Back.AllPolygons());
                return result;
            }

            public List<Polygon> ClipPolygons(List<Polygon> polygons)
            {
                var front = new List<Polygon>();
                var back = new List<Polygon>();

                foreach (var poly in polygons)
                    SplitPolygon(Plane, poly, front, back, front, back);

                if (Front != null) front = Front.ClipPolygons(front);
                if (Back != null) back = Back.ClipPolygons(back);
                else back.Clear();

                front.AddRange(back);
                return front;
            }

            public void ClipTo(BSPNode other)
            {
                Polygons = other.ClipPolygons(Polygons);
                if (Front != null) Front.ClipTo(other);
                if (Back != null) Back.ClipTo(other);
            }

            public void Invert()
            {
                for (int i = 0; i < Polygons.Count; i++)
                    Polygons[i] = Polygons[i].Flipped();

                Plane = Plane.flipped;

                if (Front != null) Front.Invert();
                if (Back != null) Back.Invert();

                (Front, Back) = (Back, Front);
            }
        }

        static void SplitPolygon(Plane plane, Polygon polygon,
            List<Polygon> coplanarFront, List<Polygon> coplanarBack,
            List<Polygon> front, List<Polygon> back)
        {
            var verts = polygon.Vertices;
            var types = new Side[verts.Count];
            Side polyType = 0;

            for (int i = 0; i < verts.Count; i++)
            {
                float dist = plane.GetDistanceToPoint(verts[i]);
                Side t = dist > Epsilon ? Side.Front : dist < -Epsilon ? Side.Back : Side.Coplanar;
                types[i] = t;
                polyType |= t;
            }

            switch (polyType)
            {
                case Side.Coplanar:
                    if (Vector3.Dot(plane.normal, polygon.Normal) > 0)
                        coplanarFront.Add(polygon);
                    else
                        coplanarBack.Add(polygon);
                    break;

                case Side.Front:
                    front.Add(polygon);
                    break;

                case Side.Back:
                    back.Add(polygon);
                    break;

                default: // Spanning
                    var f = new List<Vector3>();
                    var b = new List<Vector3>();

                    for (int i = 0; i < verts.Count; i++)
                    {
                        int j = (i + 1) % verts.Count;
                        Side ti = types[i], tj = types[j];
                        Vector3 vi = verts[i], vj = verts[j];

                        if (ti != Side.Back) f.Add(vi);
                        if (ti != Side.Front) b.Add(vi);

                        if ((ti | tj) == (Side.Front | Side.Back))
                        {
                            float dist = plane.GetDistanceToPoint(vi);
                            float denom = dist - plane.GetDistanceToPoint(vj);
                            float t = dist / denom;
                            Vector3 intersection = Vector3.Lerp(vi, vj, t);
                            f.Add(intersection);
                            b.Add(intersection);
                        }
                    }

                    if (f.Count >= 3) TriangulateFan(f, front);
                    if (b.Count >= 3) TriangulateFan(b, back);
                    break;
            }
        }

        static void TriangulateFan(List<Vector3> verts, List<Polygon> output)
        {
            for (int i = 1; i < verts.Count - 1; i++)
            {
                output.Add(new Polygon(new List<Vector3> { verts[0], verts[i], verts[i + 1] }));
            }
        }

        public static Mesh Execute(Mesh meshA, Matrix4x4 transformA, Mesh meshB, Matrix4x4 transformB, Operation op)
        {
            var polysA = MeshToPolygons(meshA, transformA);
            var polysB = MeshToPolygons(meshB, transformB);

            var a = new BSPNode(polysA);
            var b = new BSPNode(polysB);

            List<Polygon> result;

            switch (op)
            {
                case Operation.Union:
                    a.ClipTo(b);
                    b.ClipTo(a);
                    b.Invert();
                    b.ClipTo(a);
                    b.Invert();
                    a.Build(b.AllPolygons());
                    result = a.AllPolygons();
                    break;

                case Operation.Subtract:
                    a.Invert();
                    a.ClipTo(b);
                    b.ClipTo(a);
                    b.Invert();
                    b.ClipTo(a);
                    b.Invert();
                    a.Build(b.AllPolygons());
                    a.Invert();
                    result = a.AllPolygons();
                    break;

                case Operation.Intersect:
                    a.Invert();
                    b.ClipTo(a);
                    b.Invert();
                    a.ClipTo(b);
                    b.ClipTo(a);
                    a.Build(b.AllPolygons());
                    a.Invert();
                    result = a.AllPolygons();
                    break;

                default:
                    result = new List<Polygon>();
                    break;
            }

            return PolygonsToMesh(result);
        }

        static List<Polygon> MeshToPolygons(Mesh mesh, Matrix4x4 transform)
        {
            var verts = mesh.vertices;
            var tris = mesh.triangles;
            var polys = new List<Polygon>(tris.Length / 3);

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = transform.MultiplyPoint3x4(verts[tris[i]]);
                Vector3 b = transform.MultiplyPoint3x4(verts[tris[i + 1]]);
                Vector3 c = transform.MultiplyPoint3x4(verts[tris[i + 2]]);

                // Skip degenerate
                if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-10f) continue;

                polys.Add(new Polygon(new List<Vector3> { a, b, c }));
            }

            return polys;
        }

        static Mesh PolygonsToMesh(List<Polygon> polygons)
        {
            var positions = new List<Vector3>();
            var triangles = new List<int>();

            foreach (var poly in polygons)
            {
                if (poly.Vertices.Count < 3) continue;

                // Fan triangulate
                int i0 = AddVertex(positions, poly.Vertices[0]);
                for (int i = 1; i < poly.Vertices.Count - 1; i++)
                {
                    int i1 = AddVertex(positions, poly.Vertices[i]);
                    int i2 = AddVertex(positions, poly.Vertices[i + 1]);
                    triangles.Add(i0);
                    triangles.Add(i1);
                    triangles.Add(i2);
                }
            }

            var mesh = new Mesh { name = "Boolean Result" };
            if (positions.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(positions);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Weld duplicate vertices
            var em = EditableMesh.FromMesh(mesh);
            em.WeldVertices(0.0001f);
            em.ToMesh(mesh);

            return mesh;
        }

        static int AddVertex(List<Vector3> positions, Vector3 v)
        {
            int idx = positions.Count;
            positions.Add(v);
            return idx;
        }

        static int FindOrAddVertex(List<Vector3> positions, Dictionary<(int,int,int), int> cache, Vector3 v)
        {
            var key = (Mathf.RoundToInt(v.x * 10000), Mathf.RoundToInt(v.y * 10000), Mathf.RoundToInt(v.z * 10000));
            if (cache.TryGetValue(key, out int idx)) return idx;
            idx = positions.Count;
            positions.Add(v);
            cache[key] = idx;
            return idx;
        }
    }
}
#endif

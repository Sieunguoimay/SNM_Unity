#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Tools.MeshTools
{
    public static class PrimitiveGenerators
    {
        #region Plane

        public static Mesh CreatePlane(float width, float height, int segsX, int segsZ)
        {
            segsX = Mathf.Max(1, segsX);
            segsZ = Mathf.Max(1, segsZ);

            int vertsX = segsX + 1, vertsZ = segsZ + 1;
            var verts = new Vector3[vertsX * vertsZ];
            var normals = new Vector3[verts.Length];
            var uvs = new Vector2[verts.Length];

            for (int z = 0; z < vertsZ; z++)
            for (int x = 0; x < vertsX; x++)
            {
                int i = z * vertsX + x;
                float u = (float)x / segsX;
                float v = (float)z / segsZ;
                verts[i] = new Vector3((u - 0.5f) * width, 0, (v - 0.5f) * height);
                normals[i] = Vector3.up;
                uvs[i] = new Vector2(u, v);
            }

            var tris = new List<int>();
            for (int z = 0; z < segsZ; z++)
            for (int x = 0; x < segsX; x++)
            {
                int bl = z * vertsX + x;
                int br = bl + 1;
                int tl = bl + vertsX;
                int tr = tl + 1;
                tris.Add(bl); tris.Add(tl); tris.Add(tr);
                tris.Add(bl); tris.Add(tr); tris.Add(br);
            }

            return BuildMesh("Plane", verts, normals, uvs, tris.ToArray());
        }

        #endregion

        #region Box

        public static Mesh CreateBox(float width, float height, float depth, int segsX, int segsY, int segsZ)
        {
            segsX = Mathf.Max(1, segsX); segsY = Mathf.Max(1, segsY); segsZ = Mathf.Max(1, segsZ);

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            float hw = width * 0.5f, hh = height * 0.5f, hd = depth * 0.5f;

            // +Y (top)
            AddQuadFace(verts, normals, uvs, tris,
                new Vector3(-hw, hh, -hd), new Vector3(width, 0, 0), new Vector3(0, 0, depth),
                Vector3.up, segsX, segsZ);
            // -Y (bottom)
            AddQuadFace(verts, normals, uvs, tris,
                new Vector3(-hw, -hh, hd), new Vector3(width, 0, 0), new Vector3(0, 0, -depth),
                Vector3.down, segsX, segsZ);
            // +Z (front)
            AddQuadFace(verts, normals, uvs, tris,
                new Vector3(-hw, -hh, hd), new Vector3(width, 0, 0), new Vector3(0, height, 0),
                Vector3.forward, segsX, segsY);
            // -Z (back)
            AddQuadFace(verts, normals, uvs, tris,
                new Vector3(hw, -hh, -hd), new Vector3(-width, 0, 0), new Vector3(0, height, 0),
                Vector3.back, segsX, segsY);
            // +X (right)
            AddQuadFace(verts, normals, uvs, tris,
                new Vector3(hw, -hh, hd), new Vector3(0, 0, -depth), new Vector3(0, height, 0),
                Vector3.right, segsZ, segsY);
            // -X (left)
            AddQuadFace(verts, normals, uvs, tris,
                new Vector3(-hw, -hh, -hd), new Vector3(0, 0, depth), new Vector3(0, height, 0),
                Vector3.left, segsZ, segsY);

            return BuildMesh("Box", verts.ToArray(), normals.ToArray(), uvs.ToArray(), tris.ToArray());
        }

        static void AddQuadFace(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs,
            List<int> tris, Vector3 origin, Vector3 rightDir, Vector3 upDir,
            Vector3 normal, int segsU, int segsV)
        {
            int baseIdx = verts.Count;
            int vertsU = segsU + 1, vertsV = segsV + 1;

            for (int v = 0; v < vertsV; v++)
            for (int u = 0; u < vertsU; u++)
            {
                float fu = (float)u / segsU, fv = (float)v / segsV;
                verts.Add(origin + rightDir * fu + upDir * fv);
                normals.Add(normal);
                uvs.Add(new Vector2(fu, fv));
            }

            for (int v = 0; v < segsV; v++)
            for (int u = 0; u < segsU; u++)
            {
                int bl = baseIdx + v * vertsU + u;
                int br = bl + 1;
                int tl = bl + vertsU;
                int tr = tl + 1;
                tris.Add(bl); tris.Add(tl); tris.Add(tr);
                tris.Add(bl); tris.Add(tr); tris.Add(br);
            }
        }

        #endregion

        #region Sphere (UV)

        public static Mesh CreateSphere(float radius, int lonSegments, int latSegments)
        {
            lonSegments = Mathf.Max(3, lonSegments);
            latSegments = Mathf.Max(2, latSegments);

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int lat = 0; lat <= latSegments; lat++)
            {
                float theta = Mathf.PI * lat / latSegments;
                float sinT = Mathf.Sin(theta), cosT = Mathf.Cos(theta);

                for (int lon = 0; lon <= lonSegments; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / lonSegments;
                    float sinP = Mathf.Sin(phi), cosP = Mathf.Cos(phi);

                    Vector3 n = new(sinT * cosP, cosT, sinT * sinP);
                    verts.Add(n * radius);
                    normals.Add(n);
                    uvs.Add(new Vector2((float)lon / lonSegments, 1f - (float)lat / latSegments));
                }
            }

            int cols = lonSegments + 1;
            for (int lat = 0; lat < latSegments; lat++)
            for (int lon = 0; lon < lonSegments; lon++)
            {
                int curr = lat * cols + lon;
                int next = curr + cols;

                if (lat > 0)
                {
                    tris.Add(curr); tris.Add(next); tris.Add(curr + 1);
                }
                if (lat < latSegments - 1)
                {
                    tris.Add(curr + 1); tris.Add(next); tris.Add(next + 1);
                }
            }

            return BuildMesh("Sphere", verts.ToArray(), normals.ToArray(), uvs.ToArray(), tris.ToArray());
        }

        #endregion

        #region Icosphere

        public static Mesh CreateIcosphere(float radius, int subdivisions)
        {
            subdivisions = Mathf.Clamp(subdivisions, 0, 5);

            // Start with icosahedron
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var verts = new List<Vector3>
            {
                new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0),
                new(0, -1, t), new(0, 1, t), new(0, -1, -t), new(0, 1, -t),
                new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1)
            };

            for (int i = 0; i < verts.Count; i++)
                verts[i] = verts[i].normalized * radius;

            var tris = new List<int>
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };

            // Subdivide
            for (int s = 0; s < subdivisions; s++)
            {
                var midpointCache = new Dictionary<long, int>();
                var newTris = new List<int>();

                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = GetMidpoint(verts, midpointCache, a, b, radius);
                    int bc = GetMidpoint(verts, midpointCache, b, c, radius);
                    int ca = GetMidpoint(verts, midpointCache, c, a, radius);

                    newTris.Add(a); newTris.Add(ab); newTris.Add(ca);
                    newTris.Add(b); newTris.Add(bc); newTris.Add(ab);
                    newTris.Add(c); newTris.Add(ca); newTris.Add(bc);
                    newTris.Add(ab); newTris.Add(bc); newTris.Add(ca);
                }
                tris = newTris;
            }

            var positions = verts.ToArray();
            var normalsArr = new Vector3[positions.Length];
            var uvsArr = new Vector2[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                normalsArr[i] = positions[i].normalized;
                Vector3 n = normalsArr[i];
                uvsArr[i] = new Vector2(
                    0.5f + Mathf.Atan2(n.z, n.x) / (2f * Mathf.PI),
                    0.5f + Mathf.Asin(n.y) / Mathf.PI);
            }

            return BuildMesh("Icosphere", positions, normalsArr, uvsArr, tris.ToArray());
        }

        static int GetMidpoint(List<Vector3> verts, Dictionary<long, int> cache, int a, int b, float radius)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int idx)) return idx;

            Vector3 mid = ((verts[a] + verts[b]) * 0.5f).normalized * radius;
            idx = verts.Count;
            verts.Add(mid);
            cache[key] = idx;
            return idx;
        }

        #endregion

        #region Cylinder

        public static Mesh CreateCylinder(float radius, float height, int sides, int heightSegs, bool cap)
        {
            sides = Mathf.Max(3, sides);
            heightSegs = Mathf.Max(1, heightSegs);

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            float halfH = height * 0.5f;

            // Side vertices
            for (int h = 0; h <= heightSegs; h++)
            {
                float y = -halfH + height * h / heightSegs;
                float v = (float)h / heightSegs;

                for (int s = 0; s <= sides; s++)
                {
                    float angle = 2f * Mathf.PI * s / sides;
                    float x = Mathf.Cos(angle) * radius;
                    float z = Mathf.Sin(angle) * radius;
                    Vector3 normal = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

                    verts.Add(new Vector3(x, y, z));
                    normals.Add(normal);
                    uvs.Add(new Vector2((float)s / sides, v));
                }
            }

            // Side triangles
            int cols = sides + 1;
            for (int h = 0; h < heightSegs; h++)
            for (int s = 0; s < sides; s++)
            {
                int bl = h * cols + s;
                int br = bl + 1;
                int tl = bl + cols;
                int tr = tl + 1;
                tris.Add(bl); tris.Add(tl); tris.Add(tr);
                tris.Add(bl); tris.Add(tr); tris.Add(br);
            }

            // Caps
            if (cap)
            {
                AddCapFan(verts, normals, uvs, tris, radius, halfH, sides, true);
                AddCapFan(verts, normals, uvs, tris, radius, -halfH, sides, false);
            }

            return BuildMesh("Cylinder", verts.ToArray(), normals.ToArray(), uvs.ToArray(), tris.ToArray());
        }

        static void AddCapFan(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs,
            List<int> tris, float radius, float y, int sides, bool top)
        {
            Vector3 normal = top ? Vector3.up : Vector3.down;
            int centerIdx = verts.Count;

            verts.Add(new Vector3(0, y, 0));
            normals.Add(normal);
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int s = 0; s <= sides; s++)
            {
                float angle = 2f * Mathf.PI * s / sides;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                verts.Add(new Vector3(x, y, z));
                normals.Add(normal);
                uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
            }

            for (int s = 0; s < sides; s++)
            {
                int curr = centerIdx + 1 + s;
                int next = centerIdx + 1 + s + 1;
                if (top)
                {
                    tris.Add(centerIdx); tris.Add(curr); tris.Add(next);
                }
                else
                {
                    tris.Add(centerIdx); tris.Add(next); tris.Add(curr);
                }
            }
        }

        #endregion

        #region Cone

        public static Mesh CreateCone(float radiusBottom, float radiusTop, float height, int sides, int heightSegs)
        {
            sides = Mathf.Max(3, sides);
            heightSegs = Mathf.Max(1, heightSegs);

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            float halfH = height * 0.5f;
            float slopeAngle = Mathf.Atan2(radiusBottom - radiusTop, height);
            float cosSlope = Mathf.Cos(slopeAngle);
            float sinSlope = Mathf.Sin(slopeAngle);

            // Side vertices
            for (int h = 0; h <= heightSegs; h++)
            {
                float t = (float)h / heightSegs;
                float y = -halfH + height * t;
                float r = Mathf.Lerp(radiusBottom, radiusTop, t);

                for (int s = 0; s <= sides; s++)
                {
                    float angle = 2f * Mathf.PI * s / sides;
                    float cosA = Mathf.Cos(angle), sinA = Mathf.Sin(angle);

                    verts.Add(new Vector3(cosA * r, y, sinA * r));
                    normals.Add(new Vector3(cosA * cosSlope, sinSlope, sinA * cosSlope).normalized);
                    uvs.Add(new Vector2((float)s / sides, t));
                }
            }

            int cols = sides + 1;
            for (int h = 0; h < heightSegs; h++)
            for (int s = 0; s < sides; s++)
            {
                int bl = h * cols + s, br = bl + 1, tl = bl + cols, tr = tl + 1;
                tris.Add(bl); tris.Add(tl); tris.Add(tr);
                tris.Add(bl); tris.Add(tr); tris.Add(br);
            }

            // Caps
            if (radiusBottom > 0.001f)
                AddCapFan(verts, normals, uvs, tris, radiusBottom, -halfH, sides, false);
            if (radiusTop > 0.001f)
                AddCapFan(verts, normals, uvs, tris, radiusTop, halfH, sides, true);

            return BuildMesh("Cone", verts.ToArray(), normals.ToArray(), uvs.ToArray(), tris.ToArray());
        }

        #endregion

        #region Tube

        public static Mesh CreateTube(float innerRadius, float outerRadius, float height, int sides, int heightSegs)
        {
            sides = Mathf.Max(3, sides);
            heightSegs = Mathf.Max(1, heightSegs);

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvsL = new List<Vector2>();
            var tris = new List<int>();

            float halfH = height * 0.5f;

            // Outer wall
            int outerBase = verts.Count;
            AddCylinderWall(verts, norms, uvsL, tris, outerRadius, height, sides, heightSegs, true);

            // Inner wall (inward normals, reversed winding)
            int innerBase = verts.Count;
            AddCylinderWall(verts, norms, uvsL, tris, innerRadius, height, sides, heightSegs, false);

            // Top ring cap
            AddRingCap(verts, norms, uvsL, tris, innerRadius, outerRadius, halfH, sides, true);
            // Bottom ring cap
            AddRingCap(verts, norms, uvsL, tris, innerRadius, outerRadius, -halfH, sides, false);

            return BuildMesh("Tube", verts.ToArray(), norms.ToArray(), uvsL.ToArray(), tris.ToArray());
        }

        static void AddCylinderWall(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs,
            List<int> tris, float radius, float height, int sides, int heightSegs, bool outward)
        {
            int baseIdx = verts.Count;
            float halfH = height * 0.5f;
            float sign = outward ? 1f : -1f;

            for (int h = 0; h <= heightSegs; h++)
            {
                float y = -halfH + height * h / heightSegs;
                for (int s = 0; s <= sides; s++)
                {
                    float angle = 2f * Mathf.PI * s / sides;
                    float x = Mathf.Cos(angle) * radius;
                    float z = Mathf.Sin(angle) * radius;
                    verts.Add(new Vector3(x, y, z));
                    normals.Add(new Vector3(Mathf.Cos(angle) * sign, 0, Mathf.Sin(angle) * sign));
                    uvs.Add(new Vector2((float)s / sides, (float)h / heightSegs));
                }
            }

            int cols = sides + 1;
            for (int h = 0; h < heightSegs; h++)
            for (int s = 0; s < sides; s++)
            {
                int bl = baseIdx + h * cols + s, br = bl + 1, tl = bl + cols, tr = tl + 1;
                if (outward)
                {
                    tris.Add(bl); tris.Add(tl); tris.Add(tr);
                    tris.Add(bl); tris.Add(tr); tris.Add(br);
                }
                else
                {
                    tris.Add(bl); tris.Add(tr); tris.Add(tl);
                    tris.Add(bl); tris.Add(br); tris.Add(tr);
                }
            }
        }

        static void AddRingCap(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs,
            List<int> tris, float innerR, float outerR, float y, int sides, bool top)
        {
            int baseIdx = verts.Count;
            Vector3 normal = top ? Vector3.up : Vector3.down;

            // Inner ring, then outer ring
            for (int ring = 0; ring < 2; ring++)
            {
                float r = ring == 0 ? innerR : outerR;
                for (int s = 0; s <= sides; s++)
                {
                    float angle = 2f * Mathf.PI * s / sides;
                    verts.Add(new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r));
                    normals.Add(normal);
                    float u = (r == innerR) ? 0f : 1f;
                    uvs.Add(new Vector2((float)s / sides, u));
                }
            }

            int cols = sides + 1;
            for (int s = 0; s < sides; s++)
            {
                int iA = baseIdx + s, iB = baseIdx + s + 1;
                int oA = baseIdx + cols + s, oB = baseIdx + cols + s + 1;
                if (top)
                {
                    tris.Add(iA); tris.Add(oA); tris.Add(oB);
                    tris.Add(iA); tris.Add(oB); tris.Add(iB);
                }
                else
                {
                    tris.Add(iA); tris.Add(oB); tris.Add(oA);
                    tris.Add(iA); tris.Add(iB); tris.Add(oB);
                }
            }
        }

        #endregion

        #region Torus

        public static Mesh CreateTorus(float majorRadius, float minorRadius, int majorSegs, int minorSegs)
        {
            majorSegs = Mathf.Max(3, majorSegs);
            minorSegs = Mathf.Max(3, minorSegs);

            int vertCount = (majorSegs + 1) * (minorSegs + 1);
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvArr = new Vector2[vertCount];
            var tris = new List<int>();

            for (int i = 0; i <= majorSegs; i++)
            {
                float u = (float)i / majorSegs;
                float majorAngle = u * 2f * Mathf.PI;
                Vector3 center = new(Mathf.Cos(majorAngle) * majorRadius, 0, Mathf.Sin(majorAngle) * majorRadius);
                Vector3 radialDir = center.normalized;

                for (int j = 0; j <= minorSegs; j++)
                {
                    float v = (float)j / minorSegs;
                    float minorAngle = v * 2f * Mathf.PI;

                    Vector3 offset = radialDir * (Mathf.Cos(minorAngle) * minorRadius) +
                                     Vector3.up * (Mathf.Sin(minorAngle) * minorRadius);

                    int idx = i * (minorSegs + 1) + j;
                    verts[idx] = center + offset;
                    normals[idx] = offset.normalized;
                    uvArr[idx] = new Vector2(u, v);
                }
            }

            int cols = minorSegs + 1;
            for (int i = 0; i < majorSegs; i++)
            for (int j = 0; j < minorSegs; j++)
            {
                int bl = i * cols + j, br = bl + 1, tl = bl + cols, tr = tl + 1;
                tris.Add(bl); tris.Add(tl); tris.Add(tr);
                tris.Add(bl); tris.Add(tr); tris.Add(br);
            }

            return BuildMesh("Torus", verts, normals, uvArr, tris.ToArray());
        }

        #endregion

        #region Capsule

        public static Mesh CreateCapsule(float radius, float height, int segments)
        {
            segments = Mathf.Max(4, segments);
            int latHalf = segments / 2;
            float cylinderH = Mathf.Max(0, height - 2f * radius);
            float halfCylH = cylinderH * 0.5f;

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvsList = new List<Vector2>();
            var tris = new List<int>();

            int lonSegs = segments;
            float totalHeight = cylinderH + 2f * radius;

            // Top hemisphere
            for (int lat = 0; lat <= latHalf; lat++)
            {
                float theta = Mathf.PI * 0.5f * lat / latHalf;
                float sinT = Mathf.Sin(theta), cosT = Mathf.Cos(theta);
                float y = halfCylH + cosT * radius;
                float r = sinT * radius;
                float v = 1f - (y + totalHeight * 0.5f) / totalHeight;

                for (int lon = 0; lon <= lonSegs; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / lonSegs;
                    Vector3 n = new(Mathf.Cos(phi) * sinT, cosT, Mathf.Sin(phi) * sinT);
                    verts.Add(new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r));
                    normals.Add(n);
                    uvsList.Add(new Vector2((float)lon / lonSegs, 1f - v));
                }
            }

            // Cylinder section (just top and bottom rings, connected)
            if (cylinderH > 0.001f)
            {
                for (int lon = 0; lon <= lonSegs; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / lonSegs;
                    float x = Mathf.Cos(phi) * radius;
                    float z = Mathf.Sin(phi) * radius;
                    Vector3 n = new Vector3(Mathf.Cos(phi), 0, Mathf.Sin(phi));

                    verts.Add(new Vector3(x, -halfCylH, z));
                    normals.Add(n);
                    float v = (-halfCylH + totalHeight * 0.5f) / totalHeight;
                    uvsList.Add(new Vector2((float)lon / lonSegs, v));
                }
            }

            // Bottom hemisphere
            for (int lat = 0; lat <= latHalf; lat++)
            {
                float theta = Mathf.PI * 0.5f + Mathf.PI * 0.5f * lat / latHalf;
                float sinT = Mathf.Sin(theta), cosT = Mathf.Cos(theta);
                float y = -halfCylH + cosT * radius;
                float r = sinT * radius;

                for (int lon = 0; lon <= lonSegs; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / lonSegs;
                    Vector3 n = new(Mathf.Cos(phi) * sinT, cosT, Mathf.Sin(phi) * sinT);
                    verts.Add(new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r));
                    normals.Add(n);
                    float v = (y + totalHeight * 0.5f) / totalHeight;
                    uvsList.Add(new Vector2((float)lon / lonSegs, v));
                }
            }

            // Triangulate all rows
            int cols = lonSegs + 1;
            int totalRows = verts.Count / cols - 1;
            for (int row = 0; row < totalRows; row++)
            for (int lon = 0; lon < lonSegs; lon++)
            {
                int bl = row * cols + lon, br = bl + 1, tl = bl + cols, tr = tl + 1;
                tris.Add(bl); tris.Add(tl); tris.Add(tr);
                tris.Add(bl); tris.Add(tr); tris.Add(br);
            }

            return BuildMesh("Capsule", verts.ToArray(), normals.ToArray(), uvsList.ToArray(), tris.ToArray());
        }

        #endregion

        #region Helper

        static Mesh BuildMesh(string name, Vector3[] verts, Vector3[] normals, Vector2[] uvs, int[] tris)
        {
            var mesh = new Mesh { name = name };
            if (verts.Length > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.tangents = MeshGeometryUtils.ComputeTangents(verts, normals, uvs, tris);
            mesh.RecalculateBounds();
            return mesh;
        }

        #endregion
    }
}
#endif

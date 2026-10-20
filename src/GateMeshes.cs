using System.Collections.Generic;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>Builds the gate's ring and chevrune meshes in code (there's no Unity editor to model them in).</summary>
    internal static class GateMeshes
    {
        // The portal's opening is about 3 m across; these keep the ring inside the vanilla frame's footprint.
        public const float OuterRadius = 1.70f;
        public const float OuterRingIn = 1.42f;
        public const float InnerRingIn = 1.14f;
        public const float InnerRingOut = 1.42f;
        public const float OuterHalfDepth = 0.16f;
        public const float InnerHalfDepth = 0.11f;
        public const float ChevruneProtrusion = 0.05f;
        public const int ChevruneCount = 9;

        private const int Segments = 96;

        public static Mesh Annulus(string name, float radiusIn, float radiusOut, float halfDepth)
        {
            var b = new Builder();
            float reference = OuterRadius * 2f; // textures span -R..+R, so planar UVs use this

            for (int i = 0; i < Segments; i++)
            {
                float a0 = Mathf.PI * 2f * i / Segments, a1 = Mathf.PI * 2f * (i + 1) / Segments;
                Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)), d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));

                // Front and back faces (planar UVs so the texture lines up as a ring). Seen from +Z the
                // x axis runs right-to-left (Unity is left-handed), so that face mirrors u to keep runes readable.
                foreach (float z in new[] { halfDepth, -halfDepth })
                {
                    var n = new Vector3(0, 0, Mathf.Sign(z));
                    bool mirror = z > 0f;
                    b.Quad(
                        new Vector3(d0.x * radiusIn, d0.y * radiusIn, z), new Vector3(d0.x * radiusOut, d0.y * radiusOut, z),
                        new Vector3(d1.x * radiusOut, d1.y * radiusOut, z), new Vector3(d1.x * radiusIn, d1.y * radiusIn, z),
                        n, Planar(d0 * radiusIn, reference, mirror), Planar(d0 * radiusOut, reference, mirror),
                        Planar(d1 * radiusOut, reference, mirror), Planar(d1 * radiusIn, reference, mirror));
                }

                // Outer wall and inner wall.
                Wall(b, d0, d1, radiusOut, halfDepth, outward: true, reference);
                Wall(b, d0, d1, radiusIn, halfDepth, outward: false, reference);
            }
            return b.ToMesh(name);
        }

        private static void Wall(Builder b, Vector2 d0, Vector2 d1, float radius, float halfDepth, bool outward, float reference)
        {
            var n0 = new Vector3(d0.x, d0.y, 0) * (outward ? 1f : -1f);
            var n1 = new Vector3(d1.x, d1.y, 0) * (outward ? 1f : -1f);
            var p0 = d0 * radius; var p1 = d1 * radius;
            var uv0 = Planar(p0, reference); var uv1 = Planar(p1, reference);
            b.Quad(new Vector3(p0.x, p0.y, -halfDepth), new Vector3(p0.x, p0.y, halfDepth),
                   new Vector3(p1.x, p1.y, halfDepth), new Vector3(p1.x, p1.y, -halfDepth),
                   n0, n1, uv0, uv0, uv1, uv1);
        }

        private static Vector2 Planar(Vector2 p, float reference, bool mirror = false) =>
            new Vector2((mirror ? -p.x : p.x) / reference + 0.5f, p.y / reference + 0.5f);

        /// <summary>
        /// A chevrune: a "&lt;" band pointing toward the gate's centre, as a block running through the
        /// ring so it shows on both faces. Local +X is radially outward.
        /// </summary>
        public static Mesh Chevrune()
        {
            // Drawn at a convenient size, then centred (the shape runs x = -0.20..0.12) and scaled to fit the ring band.
            var shape = new[]
            {
                new Vector2( 0.12f,  0.18f), new Vector2( 0.12f,  0.10f), new Vector2(-0.02f,  0.00f),
                new Vector2( 0.12f, -0.10f), new Vector2( 0.12f, -0.18f), new Vector2(-0.20f,  0.00f),
            };
            const float scale = 0.75f;
            var poly = new Vector2[shape.Length];
            for (int i = 0; i < shape.Length; i++)
                poly[i] = new Vector2((shape[i].x + 0.04f) * scale, shape[i].y * scale);
            float h = OuterHalfDepth + ChevruneProtrusion;
            var b = new Builder();

            // Caps: the "<" splits into two convex quads.
            foreach (float z in new[] { h, -h })
            {
                var n = new Vector3(0, 0, Mathf.Sign(z));
                Cap(b, n, z, poly[0], poly[1], poly[2], poly[5]);
                Cap(b, n, z, poly[5], poly[2], poly[3], poly[4]);
            }

            // Side walls around the outline.
            float area = 0f;
            for (int i = 0; i < poly.Length; i++)
            {
                var p = poly[i]; var q = poly[(i + 1) % poly.Length];
                area += p.x * q.y - q.x * p.y;
            }
            bool ccw = area > 0f;
            for (int i = 0; i < poly.Length; i++)
            {
                var p = poly[i]; var q = poly[(i + 1) % poly.Length];
                var edge = q - p;
                var n2 = ccw ? new Vector2(edge.y, -edge.x) : new Vector2(-edge.y, edge.x);
                var n = new Vector3(n2.x, n2.y, 0).normalized;
                b.Quad(new Vector3(p.x, p.y, -h), new Vector3(p.x, p.y, h), new Vector3(q.x, q.y, h), new Vector3(q.x, q.y, -h),
                       n, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            }
            return b.ToMesh("SG1Chevrune");
        }

        private static void Cap(Builder b, Vector3 normal, float z, Vector2 a, Vector2 bb, Vector2 c, Vector2 d) =>
            b.Quad(new Vector3(a.x, a.y, z), new Vector3(bb.x, bb.y, z), new Vector3(c.x, c.y, z), new Vector3(d.x, d.y, z),
                   normal, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

        private sealed class Builder
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normals = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<int> _triangles = new List<int>();

            /// <summary>A quad, wound so it faces along <paramref name="normal"/> regardless of the corner order given.</summary>
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD) =>
                Quad(a, b, c, d, normal, normal, uvA, uvB, uvC, uvD);

            /// <summary>Same, with separate normals for the first and second edge (smooth curved walls).</summary>
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normalStart, Vector3 normalEnd,
                             Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
            {
                int start = _vertices.Count;
                _vertices.AddRange(new[] { a, b, c, d });
                _normals.AddRange(new[] { normalStart, normalStart, normalEnd, normalEnd });
                _uvs.AddRange(new[] { uvA, uvB, uvC, uvD });

                var face = (normalStart + normalEnd).normalized;
                Tri(start, start + 1, start + 2, face);
                Tri(start, start + 2, start + 3, face);
            }

            private void Tri(int i0, int i1, int i2, Vector3 facing)
            {
                var cross = Vector3.Cross(_vertices[i1] - _vertices[i0], _vertices[i2] - _vertices[i0]);
                if (Vector3.Dot(cross, facing) < 0f) { int t = i1; i1 = i2; i2 = t; }
                _triangles.Add(i0); _triangles.Add(i1); _triangles.Add(i2);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetUVs(0, _uvs);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                return mesh;
            }
        }
    }
}

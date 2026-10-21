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

        /// <summary>A flat disc facing along Z, with UVs mapped across it (the water fills this).</summary>
        public static Mesh Disc(float radius, int segments)
        {
            var vertices = new List<Vector3> { Vector3.zero };
            var uvs = new List<Vector2> { new Vector2(0.5f, 0.5f) };
            var normals = new List<Vector3> { new Vector3(0, 0, 1) };
            var triangles = new List<int>();

            for (int i = 0; i < segments; i++)
            {
                float a = Mathf.PI * 2f * i / segments;
                float x = Mathf.Cos(a), y = Mathf.Sin(a);
                vertices.Add(new Vector3(x * radius, y * radius, 0f));
                uvs.Add(new Vector2(0.5f + x * 0.5f, 0.5f + y * 0.5f));
                normals.Add(new Vector3(0, 0, 1));
                int next = 1 + (i + 1) % segments;
                triangles.Add(0); triangles.Add(1 + i); triangles.Add(next);
            }

            var mesh = new Mesh { name = "SG1Water" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---------------------------------------------------------------- ramps

        /// <summary>
        /// A wedge that rises from below ground out in front of the portal up to the top of its base slab, so
        /// walking in is a gentle slope even for someone overencumbered. Drawn on the +Z side (rotate it for the back).
        /// Doubles as the collision shape (it's convex).
        /// </summary>
        public static Mesh Ramp(float halfWidth, float zStart, float zEnd, float yTop, float yBottom)
        {
            var b = new Builder();
            float w = halfWidth;
            var topL = new Vector3(-w, yTop, zStart);    var topR = new Vector3(w, yTop, zStart);
            var farL = new Vector3(-w, yBottom, zEnd);   var farR = new Vector3(w, yBottom, zEnd);
            var botL = new Vector3(-w, yBottom, zStart); var botR = new Vector3(w, yBottom, zStart);

            // The slope: up and outward, perpendicular to the way it runs.
            float rise = yTop - yBottom, run = zEnd - zStart;
            float len = Mathf.Sqrt(rise * rise + run * run);
            var slope = new Vector3(0f, run / len, rise / len);

            Vector2 Uv(Vector3 p) => new Vector2(0.5f + p.x * 0.25f, 0.5f + p.z * 0.25f);
            void Face(Vector3 a, Vector3 bb, Vector3 c, Vector3 d, Vector3 n) => b.Quad(a, bb, c, d, n, Uv(a), Uv(bb), Uv(c), Uv(d));

            Face(topL, topR, farR, farL, slope);                          // the walkable slope
            Face(topL, topR, botR, botL, new Vector3(0, 0, -1));          // back, against the slab
            Face(botL, botR, farR, farL, new Vector3(0, -1, 0));          // underneath
            Face(topL, farL, botL, botL, new Vector3(-1, 0, 0));          // left end (a triangle)
            Face(topR, farR, botR, botR, new Vector3(1, 0, 0));           // right end (a triangle)
            return b.ToMesh("SG1Ramp");
        }

        // ---------------------------------------------------------------- runes

        // How big a rune is on the ring, and how thick its strokes and raised plates are.
        private const float RuneScale = 0.125f;
        private const float RuneStroke = 0.034f;
        private const float RunePlate = 0.035f;

        /// <summary>
        /// A rune as raised strokes on both faces of the ring: the front plate reads correctly from the +Z side
        /// and the back plate (mirrored) from the -Z side. Local +X is radially outward, like the chevrune arrow.
        /// </summary>
        public static Mesh Rune(int glyph)
        {
            var b = new Builder();
            float outer = OuterHalfDepth + ChevruneProtrusion;
            foreach (var line in Art.Runes.Lines(glyph))
            {
                for (int k = 0; k + 3 < line.Length; k += 2)
                {
                    // Glyph "up" runs radially outward, glyph "right" runs along the ring (as on the inner ring).
                    var p = new Vector2((line[k + 1] - 1f) * RuneScale, (line[k] - 0.5f) * RuneScale * 1.2f);
                    var q = new Vector2((line[k + 3] - 1f) * RuneScale, (line[k + 2] - 0.5f) * RuneScale * 1.2f);
                    Stroke(b, p, q, outer, outer - RunePlate, mirror: false);
                    Stroke(b, p, q, -outer, -outer + RunePlate, mirror: true);
                }
            }
            return b.ToMesh($"SG1Rune{glyph}");
        }

        /// <summary>One straight stroke as a thin raised bar, with squared ends that overlap neighbours at the joints.</summary>
        private static void Stroke(Builder b, Vector2 p, Vector2 q, float zOuter, float zInner, bool mirror)
        {
            if (mirror) { p = new Vector2(p.x, -p.y); q = new Vector2(q.x, -q.y); }
            float dx = q.x - p.x, dy = q.y - p.y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 1e-5f) return;
            float ux = dx / len, uy = dy / len;        // along the stroke
            float nx = -uy, ny = ux;                    // across it
            float half = RuneStroke * 0.5f;

            var corners = new[]
            {
                new Vector2(p.x - ux * half + nx * half, p.y - uy * half + ny * half),
                new Vector2(q.x + ux * half + nx * half, q.y + uy * half + ny * half),
                new Vector2(q.x + ux * half - nx * half, q.y + uy * half - ny * half),
                new Vector2(p.x - ux * half - nx * half, p.y - uy * half - ny * half),
            };

            // Top face (the one you see) and the four sides down to the ring.
            var top = new Vector3(0, 0, Mathf.Sign(zOuter));
            b.Quad(new Vector3(corners[0].x, corners[0].y, zOuter), new Vector3(corners[1].x, corners[1].y, zOuter),
                   new Vector3(corners[2].x, corners[2].y, zOuter), new Vector3(corners[3].x, corners[3].y, zOuter),
                   top, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

            for (int i = 0; i < 4; i++)
            {
                var a = corners[i]; var c = corners[(i + 1) % 4];
                float ex = c.x - a.x, ey = c.y - a.y, el = Mathf.Sqrt(ex * ex + ey * ey);
                // Outward from the bar's centre, whichever way the corners were wound.
                var mid = new Vector2((corners[0].x + corners[2].x) * 0.5f, (corners[0].y + corners[2].y) * 0.5f);
                var normal = new Vector3(ey / el, -ex / el, 0f);
                if (normal.x * ((a.x + c.x) * 0.5f - mid.x) + normal.y * ((a.y + c.y) * 0.5f - mid.y) < 0f) normal = normal * -1f;
                b.Quad(new Vector3(a.x, a.y, zInner), new Vector3(a.x, a.y, zOuter), new Vector3(c.x, c.y, zOuter), new Vector3(c.x, c.y, zInner),
                       normal, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            }
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

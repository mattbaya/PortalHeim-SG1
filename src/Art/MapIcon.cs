using System;

namespace PortalHeimSG1.Art
{
    /// <summary>
    /// The little gate that marks a gate on the map. Like the game's own map icons it is a flat, single-colour shape:
    /// a ring with the nine Chevrunes cut into it as arrows pointing in, and a dot of "water" in the middle.
    /// Drawn in code like the rest of the artwork.
    /// </summary>
    public static class MapIcon
    {
        public const int Size = 128;

        // The pale warm white the game's own pins use.
        private const byte R = 244, G = 236, B = 214;

        public static Pixels Draw()
        {
            var px = new Pixels(Size, Size);
            float c = (Size - 1) * 0.5f;
            const float outer = 60f, inner = 37f, dot = 15f;

            // Nine arrows, the top one first, each pointing at the centre.
            var tris = new float[9][];
            for (int k = 0; k < 9; k++)
            {
                float angle = (90f - k * 40f) * (float)Math.PI / 180f;
                float ux = (float)Math.Cos(angle), uy = (float)Math.Sin(angle);
                float tx = -uy, ty = ux;
                float tip = 41f, baseR = 55f, half = 7f;
                tris[k] = new[]
                {
                    c + ux * tip, c + uy * tip,
                    c + ux * baseR + tx * half, c + uy * baseR + ty * half,
                    c + ux * baseR - tx * half, c + uy * baseR - ty * half,
                };
            }

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = x - c, dy = y - c;
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);

                    float ring = Cover(outer - d) * Cover(d - inner);
                    float cut = 0f;
                    foreach (var t in tris) cut = Math.Max(cut, InsideTriangle(x, y, t));
                    float shape = Math.Max(ring * (1f - cut), Cover(dot - d));

                    px.Blend(x, y, R, G, B, shape);
                }
            }

            return px;
        }

        /// <summary>Anti-aliased edge: 1 well inside, 0 well outside.</summary>
        private static float Cover(float signedDistance) => Math.Max(0f, Math.Min(1f, signedDistance + 0.5f));

        /// <summary>1 inside the triangle, 0 outside, soft across the edge.</summary>
        private static float InsideTriangle(float px, float py, float[] t)
        {
            // Signed distance to each edge (negative inside), taking the worst one.
            float area = (t[2] - t[0]) * (t[5] - t[1]) - (t[4] - t[0]) * (t[3] - t[1]);
            float sign = area >= 0f ? 1f : -1f;
            float worst = float.MinValue;
            for (int i = 0; i < 3; i++)
            {
                int j = (i + 1) % 3;
                float ex = t[j * 2] - t[i * 2], ey = t[j * 2 + 1] - t[i * 2 + 1];
                float length = (float)Math.Sqrt(ex * ex + ey * ey);
                // Distance from the edge, positive on the outside.
                float dist = -sign * (ex * (py - t[i * 2 + 1]) - ey * (px - t[i * 2])) / length;
                worst = Math.Max(worst, dist);
            }

            return Cover(-worst);
        }
    }
}

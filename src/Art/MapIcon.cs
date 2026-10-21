using System;

namespace PortalHeimSG1.Art
{
    /// <summary>
    /// The little gate that marks a gate on the map. Like the game's own map icons it is a flat, single-colour shape:
    /// the gate as it looks when powered off (an empty ring with the nine Chevrunes as arrows cut into it), sunk into the
    /// ground, with a small ramp in front. Drawn in code like the rest of the artwork.
    /// </summary>
    public static class MapIcon
    {
        public const int Size = 128;

        // The pale warm white the game's own pins use.
        private const byte R = 244, G = 236, B = 214;

        public static Pixels Draw()
        {
            var px = new Pixels(Size, Size);
            float cx = (Size - 1) * 0.5f, cy = 71f;
            const float outer = 56f, inner = 35f;
            // The ring is buried below this line; the ramp sits under it. (Origin is the bottom-left.)
            const float ground = 27f;

            // Nine arrows, the top one first, each pointing at the centre.
            var tris = new float[9][];
            for (int k = 0; k < 9; k++)
            {
                float angle = (90f - k * 40f) * (float)Math.PI / 180f;
                float ux = (float)Math.Cos(angle), uy = (float)Math.Sin(angle);
                float tx = -uy, ty = ux;
                float tip = 38.5f, baseR = 52f, half = 6.6f;
                tris[k] = new[]
                {
                    cx + ux * tip, cy + uy * tip,
                    cx + ux * baseR + tx * half, cy + uy * baseR + ty * half,
                    cx + ux * baseR - tx * half, cy + uy * baseR - ty * half,
                };
            }

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);

                    float ring = Cover(outer - d) * Cover(d - inner) * Cover(y - ground);
                    float cut = 0f;
                    foreach (var t in tris) cut = Math.Max(cut, InsideTriangle(x, y, t));
                    px.Blend(x, y, R, G, B, ring * (1f - cut));
                }
            }

            // The ramp: a low wedge in front of the buried base, wider at the bottom.
            px.FillConvex(new[] { 32f, 96f, 118f, 10f }, new[] { 20f, 20f, 5f, 5f }, R, G, B);

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

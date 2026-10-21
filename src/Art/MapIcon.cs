using System;

namespace PortalHeimSG1.Art
{
    /// <summary>
    /// The little gate that marks a gate on the map: the stone ring, nine orange Chevrunes pointing in, and light-blue water inside,
    /// with a dark outline so it stands out on the parchment. Drawn in code like the rest of the artwork.
    /// </summary>
    public static class MapIcon
    {
        public const int Size = 128;

        public static Pixels Draw()
        {
            var px = new Pixels(Size, Size);
            float c = (Size - 1) * 0.5f;
            const float outer = 60f, inner = 38f, outline = 2.6f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = x - c, dy = y - c;
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);

                    // Dark outline hugging the outside of the ring (and the inside edge, so the water is framed too).
                    px.Blend(x, y, 24, 18, 12, Cover(outer + outline - d) * 0.95f);

                    // The stone: a little lighter at the top left, like light falling on it.
                    float light = 0.5f + 0.5f * (-dx * 0.6f + dy * 0.8f) / Math.Max(d, 1f);
                    byte sr = (byte)(112 + 46 * light), sg = (byte)(106 + 42 * light), sb = (byte)(98 + 36 * light);
                    px.Blend(x, y, sr, sg, sb, Cover(outer - d) * Cover(d - inner));

                    // The water, brighter in the middle.
                    float depth = Math.Max(0f, 1f - d / inner);
                    byte wr = (byte)(96 + 56 * depth), wg = (byte)(176 + 44 * depth), wb = (byte)(230 + 20 * depth);
                    px.Blend(x, y, wr, wg, wb, Cover(inner - d));
                }
            }

            // The Chevrunes: nine small arrows sitting on the ring, the top one first, pointing at the centre.
            for (int k = 0; k < 9; k++)
            {
                float angle = (90f - k * 40f) * (float)Math.PI / 180f;
                float ux = (float)Math.Cos(angle), uy = (float)Math.Sin(angle);   // outward
                float tx = -uy, ty = ux;                                           // along the ring
                float tip = 41.5f, baseR = 57f, half = 7.2f;
                var xs = new[] { c + ux * tip, c + ux * baseR + tx * half, c + ux * baseR - tx * half };
                var ys = new[] { c + uy * tip, c + uy * baseR + ty * half, c + uy * baseR - ty * half };

                // A darker, slightly larger shape under it gives each Chevrune an edge.
                float grow = 1.7f;
                var gx = new[] { c + ux * (tip - grow), c + ux * (baseR + grow) + tx * (half + grow), c + ux * (baseR + grow) - tx * (half + grow) };
                var gy = new[] { c + uy * (tip - grow), c + uy * (baseR + grow) + ty * (half + grow), c + uy * (baseR + grow) - ty * (half + grow) };
                px.FillConvex(gx, gy, 70, 30, 6);
                px.FillConvex(xs, ys, 255, 150, 40);
            }

            return px;
        }

        /// <summary>Anti-aliased edge: 1 well inside, 0 well outside.</summary>
        private static float Cover(float signedDistance) => Math.Max(0f, Math.Min(1f, signedDistance + 0.5f));
    }
}

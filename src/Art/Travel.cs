using System;

namespace PortalHeimSG1.Art
{
    /// <summary>
    /// A flat, miniature gate for the teleport loading screen: a ring of runes (drawn separately so it can
    /// turn) inside a frame with nine lit Chevrunes. Both have transparent backgrounds. The water goes in the middle.
    /// </summary>
    public static class Travel
    {
        public const int Size = 512;
        // Ring radii as a fraction of half the image.
        public const float WaterRadius = 0.56f;
        public const float RuneIn = 0.58f, RuneOut = 0.80f;
        public const float FrameIn = 0.81f, FrameOut = 0.98f;
        public const int ChevruneCount = 9;

        public static Pixels RuneRing()
        {
            var c = new Pixels(Size, Size);
            float half = Size / 2f;
            Annulus(c, RuneIn * half, RuneOut * half, 112, 104, 92, speckle: 16, seed: 11);
            Runes.PaintRing(c, half, half, RuneIn * half, RuneOut * half, 36, 3);
            return c;
        }

        public static Pixels Frame(byte glowR = 255, byte glowG = 115, byte glowB = 26)
        {
            var c = new Pixels(Size, Size);
            float half = Size / 2f;
            Annulus(c, FrameIn * half, FrameOut * half, 96, 90, 80, speckle: 12, seed: 5);

            // Nine lit Chevrunes sitting in the frame, the first at the top.
            float mid = (FrameIn + FrameOut) * 0.5f * half;
            float scale = (FrameOut - FrameIn) * half / 0.32f * 0.78f; // the chevrune shape is about 0.32 across
            for (int i = 0; i < ChevruneCount; i++)
            {
                double angle = Math.PI / 2.0 + 2.0 * Math.PI * i / ChevruneCount;
                Chevrune(c, half, half, mid, angle, scale, glowR, glowG, glowB);
            }
            return c;
        }

        // The chevrune "<" as two convex quads, in the same units as GateMeshes.Chevrune, centred and pointing inward.
        private static readonly float[][] Quads =
        {
            new[] { 0.12f,  0.18f,  0.12f,  0.10f, -0.02f, 0.00f, -0.20f, 0.00f },
            new[] {-0.20f,  0.00f, -0.02f,  0.00f,  0.12f,-0.10f,  0.12f,-0.18f },
        };

        private static void Chevrune(Pixels c, float cx, float cy, float radius, double angle, float scale, byte r, byte g, byte b)
        {
            float cos = (float)Math.Cos(angle), sin = (float)Math.Sin(angle);
            foreach (var q in Quads)
            {
                var xs = new float[4]; var ys = new float[4];
                for (int k = 0; k < 4; k++)
                {
                    // Local x runs radially outward, y along the ring; centre the shape (it spans x = -0.20..0.12).
                    float lx = (q[k * 2] + 0.04f) * scale, ly = q[k * 2 + 1] * scale;
                    xs[k] = cx + cos * (radius + lx) - sin * ly;
                    ys[k] = cy + sin * (radius + lx) + cos * ly;
                }
                c.FillConvex(xs, ys, r, g, b);
            }
        }

        private static void Annulus(Pixels c, float rIn, float rOut, byte r, byte g, byte b, float speckle, int seed)
        {
            float cx = c.Width / 2f, cy = c.Height / 2f;
            var rng = new Random(seed);
            for (int y = 0; y < c.Height; y++)
            {
                for (int x = 0; x < c.Width; x++)
                {
                    float d = (float)Math.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    // Anti-aliased edges, 1.5 px soft.
                    float cover = Math.Min(Clamp01((d - rIn) / 1.5f + 0.5f), Clamp01((rOut - d) / 1.5f + 0.5f));
                    if (cover <= 0f) continue;
                    float n = (float)((rng.NextDouble() - 0.5) * 2.0 * speckle);
                    // A touch of shading: lighter toward the outer edge.
                    float shade = (d - rIn) / (rOut - rIn) * 14f - 7f;
                    c.Blend(x, y, Clamp(r + n + shade), Clamp(g + n + shade), Clamp(b + n + shade), cover);
                }
            }
        }

        private static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
        private static byte Clamp(float v) => (byte)Math.Max(0, Math.Min(255, v));
    }
}

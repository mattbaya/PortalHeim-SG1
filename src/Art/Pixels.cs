using System;

namespace PortalHeimSG1.Art
{
    /// <summary>
    /// A tiny RGBA drawing surface with no Unity dependency, so the artwork can be generated and
    /// previewed outside the game. Origin is the bottom-left (like Unity textures).
    /// </summary>
    public sealed class Pixels
    {
        public readonly int Width;
        public readonly int Height;
        public readonly byte[] Rgba;

        public Pixels(int width, int height)
        {
            Width = width;
            Height = height;
            Rgba = new byte[width * height * 4];
        }

        public void Fill(byte r, byte g, byte b, byte a)
        {
            for (int i = 0; i < Rgba.Length; i += 4)
            {
                Rgba[i] = r; Rgba[i + 1] = g; Rgba[i + 2] = b; Rgba[i + 3] = a;
            }
        }

        /// <summary>Blends a colour over one pixel with the given coverage (0-1).</summary>
        public void Blend(int x, int y, byte r, byte g, byte b, float coverage)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height || coverage <= 0f) return;
            if (coverage > 1f) coverage = 1f;
            int i = (y * Width + x) * 4;
            // Standard "over": draws on top of whatever is there, keeping transparency where nothing was painted.
            float dstA = Rgba[i + 3] / 255f;
            float outA = coverage + dstA * (1f - coverage);
            if (outA <= 0f) return;
            Rgba[i] = (byte)((r * coverage + Rgba[i] * dstA * (1f - coverage)) / outA);
            Rgba[i + 1] = (byte)((g * coverage + Rgba[i + 1] * dstA * (1f - coverage)) / outA);
            Rgba[i + 2] = (byte)((b * coverage + Rgba[i + 2] * dstA * (1f - coverage)) / outA);
            Rgba[i + 3] = (byte)Math.Round(outA * 255f);
        }

        /// <summary>A line with rounded ends, anti-aliased by distance to the segment.</summary>
        public void Line(float x0, float y0, float x1, float y1, float width, byte r, byte g, byte b, float strength = 1f)
        {
            float half = width * 0.5f;
            int minX = (int)Math.Floor(Math.Min(x0, x1) - half - 1);
            int maxX = (int)Math.Ceiling(Math.Max(x0, x1) + half + 1);
            int minY = (int)Math.Floor(Math.Min(y0, y1) - half - 1);
            int maxY = (int)Math.Ceiling(Math.Max(y0, y1) + half + 1);
            float dx = x1 - x0, dy = y1 - y0;
            float lenSq = dx * dx + dy * dy;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float px = x + 0.5f - x0, py = y + 0.5f - y0;
                    float t = lenSq <= 0f ? 0f : Math.Max(0f, Math.Min(1f, (px * dx + py * dy) / lenSq));
                    float cx = px - dx * t, cy = py - dy * t;
                    float dist = (float)Math.Sqrt(cx * cx + cy * cy);
                    Blend(x, y, r, g, b, (half + 0.5f - dist) * strength);
                }
            }
        }

        /// <summary>Fills a convex polygon (corners in order) with 4x4 supersampled edges.</summary>
        public void FillConvex(float[] xs, float[] ys, byte r, byte g, byte b)
        {
            int n = xs.Length;
            int minX = (int)Math.Floor(Min(xs)), maxX = (int)Math.Ceiling(Max(xs));
            int minY = (int)Math.Floor(Min(ys)), maxY = (int)Math.Ceiling(Max(ys));
            // Orientation of the polygon, so "inside" works for either winding.
            float area = 0f;
            for (int i = 0; i < n; i++) { int j = (i + 1) % n; area += xs[i] * ys[j] - xs[j] * ys[i]; }
            float sign = area >= 0f ? 1f : -1f;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < 4; sy++)
                    for (int sx = 0; sx < 4; sx++)
                    {
                        float px = x + (sx + 0.5f) / 4f, py = y + (sy + 0.5f) / 4f;
                        bool all = true;
                        for (int i = 0; i < n && all; i++)
                        {
                            int j = (i + 1) % n;
                            float cross = (xs[j] - xs[i]) * (py - ys[i]) - (ys[j] - ys[i]) * (px - xs[i]);
                            if (cross * sign < 0f) all = false;
                        }
                        if (all) inside++;
                    }
                    if (inside > 0) Blend(x, y, r, g, b, inside / 16f);
                }
            }
        }

        private static float Min(float[] v) { float m = v[0]; foreach (var f in v) if (f < m) m = f; return m; }
        private static float Max(float[] v) { float m = v[0]; foreach (var f in v) if (f > m) m = f; return m; }

        /// <summary>Gentle mottled noise, to take the flatness off stone.</summary>
        public void Speckle(int seed, float amount)
        {
            var rng = new Random(seed);
            for (int i = 0; i < Rgba.Length; i += 4)
            {
                if (Rgba[i + 3] == 0) continue;
                int delta = (int)((rng.NextDouble() - 0.5) * 2.0 * amount);
                Rgba[i] = Clamp(Rgba[i] + delta);
                Rgba[i + 1] = Clamp(Rgba[i + 1] + delta);
                Rgba[i + 2] = Clamp(Rgba[i + 2] + delta);
            }
        }

        private static byte Clamp(int v) => (byte)Math.Max(0, Math.Min(255, v));
    }
}

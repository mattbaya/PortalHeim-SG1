using System;

namespace PortalHeimSG1.Art
{
    /// <summary>
    /// A tiny RGBA drawing surface with no Unity dependency, so the artwork can be generated and
    /// previewed outside the game. Origin is the bottom-left (like Unity textures).
    /// </summary>
    public sealed class Canvas
    {
        public readonly int Width;
        public readonly int Height;
        public readonly byte[] Rgba;

        public Canvas(int width, int height)
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
            Rgba[i] = (byte)(Rgba[i] + (r - Rgba[i]) * coverage);
            Rgba[i + 1] = (byte)(Rgba[i + 1] + (g - Rgba[i + 1]) * coverage);
            Rgba[i + 2] = (byte)(Rgba[i + 2] + (b - Rgba[i + 2]) * coverage);
            Rgba[i + 3] = 255;
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

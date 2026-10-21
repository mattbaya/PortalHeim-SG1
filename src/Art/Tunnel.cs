using System;

namespace PortalHeimSG1.Art
{
    /// <summary>
    /// An endless tunnel of swirling cloud, flying toward a bright light: the "time vortex" look. Painted in code
    /// (no Unity types, so it can be previewed and generated on a background thread). Frames loop seamlessly: the
    /// cloud pattern repeats exactly once along the tunnel's length over the whole set.
    /// </summary>
    public static class Tunnel
    {
        public const int Frames = 36;
        public const int Width = 384, Height = 216;

        public static Pixels Frame(int frame)
        {
            var img = new Pixels(Width, Height);
            double t = (double)frame / Frames; // 0..1: one full period of travel down the tunnel

            for (int py = 0; py < Height; py++)
            {
                for (int px = 0; px < Width; px++)
                {
                    // Centre of the screen is the far end of the tunnel; r = 1 at the top and bottom edge.
                    double x = (px + 0.5 - Width / 2.0) / (Height / 2.0);
                    double y = (py + 0.5 - Height / 2.0) / (Height / 2.0);
                    double r = Math.Sqrt(x * x + y * y);
                    double angle = Math.Atan2(y, x);

                    // Perspective: the wall's depth grows as 1/r. It's drawn in "tunnel space" (u around, v along).
                    double depth = 0.42 / (r + 0.10);
                    double u = angle / (2.0 * Math.PI) + 0.07 * depth;   // a gentle twist down the tunnel
                    double v = depth + t;                                 // moving forward

                    // Billowing cloud: noise, warped by more noise so it swirls rather than sits in rings.
                    double wu = Noise(u, v, 4, 3, 91) - 0.5, wv = Noise(u, v, 4, 3, 17) - 0.5;
                    double uu = u + 0.22 * wu, vv = v + 0.30 * wv;
                    double cloud = Fbm(uu, vv, 6, 3, 5);
                    // Bright electric veins where the noise folds back on itself.
                    double vein = 1.0 - Math.Abs(2.0 * Fbm(uu * 1.0, vv, 7, 4, 41) - 1.0);
                    vein = Math.Pow(vein, 6.0);

                    double c = Clamp01((cloud - 0.28) / 0.55) * 0.9 + vein * 0.55;
                    // Nearer the middle (the far end) it's brighter, thinning into light.
                    double toward = Clamp01(1.0 - r / 1.55);
                    c = c * (0.50 + 0.55 * toward);

                    Ramp(Clamp01(c), out double cr, out double cg, out double cb);

                    // The light at the end of the tunnel, with a warm edge.
                    double core = Math.Exp(-(r * r) / (0.05 * 0.05 + 1e-6));
                    double halo = Math.Exp(-(r * r) / (0.30 * 0.30));
                    cr += core * 255 + halo * 70; cg += core * 250 + halo * 55; cb += core * 235 + halo * 35;

                    // Dark corners, as if the tunnel closes in around the edges.
                    double vignette = 1.0 - 0.45 * SmoothStep(0.9, 2.0, r);
                    int i = (py * Width + px) * 4;
                    img.Rgba[i] = ToByte(cr * vignette);
                    img.Rgba[i + 1] = ToByte(cg * vignette);
                    img.Rgba[i + 2] = ToByte(cb * vignette);
                    img.Rgba[i + 3] = 255;
                }
            }
            return img;
        }

        // ---------------------------------------------------------------- colour

        private static readonly double[][] Stops =
        {
            new[] { 0.00,   6,   8,  44 },
            new[] { 0.30,  26,  36, 140 },
            new[] { 0.55,  70, 120, 225 },
            new[] { 0.78, 165, 210, 255 },
            new[] { 1.00, 255, 250, 242 },
        };

        private static void Ramp(double t, out double r, out double g, out double b)
        {
            for (int i = 1; i < Stops.Length; i++)
            {
                if (t <= Stops[i][0])
                {
                    double f = (t - Stops[i - 1][0]) / (Stops[i][0] - Stops[i - 1][0]);
                    r = Stops[i - 1][1] + (Stops[i][1] - Stops[i - 1][1]) * f;
                    g = Stops[i - 1][2] + (Stops[i][2] - Stops[i - 1][2]) * f;
                    b = Stops[i - 1][3] + (Stops[i][3] - Stops[i - 1][3]) * f;
                    return;
                }
            }
            r = Stops[Stops.Length - 1][1]; g = Stops[Stops.Length - 1][2]; b = Stops[Stops.Length - 1][3];
        }

        // ---------------------------------------------------------------- periodic noise (repeats every 1.0 in u and v)

        private static double Fbm(double u, double v, int lu, int lv, int seed)
        {
            double sum = 0, amp = 0.5, norm = 0;
            for (int o = 0; o < 4; o++)
            {
                sum += amp * Noise(u, v, lu << o, lv << o, seed + o * 13);
                norm += amp;
                amp *= 0.5;
            }
            return sum / norm;
        }

        /// <summary>Smooth value noise on an lu x lv lattice that wraps, so it tiles seamlessly across 0..1 in both directions.</summary>
        private static double Noise(double u, double v, int lu, int lv, int seed)
        {
            u -= Math.Floor(u); v -= Math.Floor(v);
            double x = u * lu, y = v * lv;
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
            double fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int x1 = (x0 + 1) % lu, y1 = (y0 + 1) % lv;
            x0 %= lu; y0 %= lv;
            double a = Hash(x0, y0, seed), b = Hash(x1, y0, seed), c = Hash(x0, y1, seed), d = Hash(x1, y1, seed);
            return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
        }

        private static double Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (double)0x1000000;
            }
        }

        private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;

        private static double SmoothStep(double e0, double e1, double x)
        {
            double t = Clamp01((x - e0) / (e1 - e0));
            return t * t * (3 - 2 * t);
        }

        private static byte ToByte(double v) => (byte)Math.Max(0, Math.Min(255, Math.Round(v)));
    }
}

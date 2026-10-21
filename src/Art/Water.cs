using System;

namespace PortalHeimSG1.Art
{
    /// <summary>
    /// Frames of a rippling pool of water, painted in code (no Unity types, so they can be previewed
    /// outside the game). Played in a loop they look like ripples spreading across a still pool, with
    /// glints where the surface catches the light. The first and last frames line up, so it loops cleanly.
    /// </summary>
    public static class Water
    {
        public const int Size = 256;
        public const int Frames = 24;

        // Each ripple source: where it is, how many rings per unit it makes, how many cycles per loop, and how strong.
        private static readonly float[][] Sources =
        {
            //  x      y     rings  cycles amplitude
            new[] { 0.00f,  0.00f, 2.6f, 2f, 0.90f },   // the main rings, spreading out from the middle
            new[] {-0.45f,  0.30f, 3.6f, 1f, 0.32f },   // two gentler ripples from off to the sides
            new[] { 0.42f, -0.36f, 4.2f, 1f, 0.24f },
        };

        public static Pixels Frame(int frame)
        {
            var c = new Pixels(Size, Size);
            double phase = 2.0 * Math.PI * frame / Frames;
            const double eps = 0.01;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    double u = (x + 0.5) / Size * 2.0 - 1.0, v = (y + 0.5) / Size * 2.0 - 1.0;
                    double r = Math.Sqrt(u * u + v * v);
                    int i = (y * Size + x) * 4;

                    if (r >= 1.0)
                    {
                        // Outside the disc: transparent, but in the water's colour so edges don't fringe dark.
                        c.Rgba[i] = 8; c.Rgba[i + 1] = 52; c.Rgba[i + 2] = 100; c.Rgba[i + 3] = 0;
                        continue;
                    }

                    double h = Height(u, v, phase);
                    double dhdu = (Height(u + eps, v, phase) - Height(u - eps, v, phase)) / (2 * eps);
                    double dhdv = (Height(u, v + eps, phase) - Height(u, v - eps, phase)) / (2 * eps);

                    // Surface normal from the slope, lit from the upper left; a glint where it faces the viewer.
                    const double slope = 0.11;
                    double nx = -dhdu * slope, ny = -dhdv * slope, nz = 1.0;
                    double nl = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                    nx /= nl; ny /= nl; nz /= nl;
                    double lx = -0.4, ly = 0.6, lz = 0.7, ll = Math.Sqrt(lx * lx + ly * ly + lz * lz);
                    lx /= ll; ly /= ll; lz /= ll;
                    double diffuse = Math.Max(0.0, nx * lx + ny * ly + nz * lz);
                    // Half vector between the light and the viewer (0,0,1).
                    double hx = lx, hy = ly, hz = lz + 1.0, hl = Math.Sqrt(hx * hx + hy * hy + hz * hz);
                    double spec = Math.Pow(Math.Max(0.0, (nx * hx + ny * hy + nz * hz) / hl), 90.0);

                    // Deep blue at the rim, bright turquoise toward the middle, lifted where ripples catch the light.
                    double t = Clamp01(0.32 + 0.30 * (1.0 - r) + 0.26 * h + 0.30 * diffuse * diffuse);
                    Gradient(t, out double cr, out double cg, out double cb);
                    cr += spec * 255.0 * 0.85; cg += spec * 255.0 * 0.85; cb += spec * 255.0 * 0.85;

                    double edge = SmoothStep(1.0, 0.90, r);
                    double alpha = (0.80 + 0.20 * Clamp01(spec * 2.0)) * edge;

                    c.Rgba[i] = ToByte(cr); c.Rgba[i + 1] = ToByte(cg); c.Rgba[i + 2] = ToByte(cb); c.Rgba[i + 3] = ToByte(alpha * 255.0);
                }
            }
            return c;
        }

        /// <summary>Surface height at a point: the sum of rings spreading out from each source.</summary>
        private static double Height(double u, double v, double phase)
        {
            double h = 0.0;
            foreach (var s in Sources)
            {
                double dx = u - s[0], dy = v - s[1];
                double d = Math.Sqrt(dx * dx + dy * dy);
                // Whole cycles per loop (s[3]) keep the loop seamless; the fade keeps far ripples gentle.
                h += s[4] * Math.Sin(2.0 * Math.PI * s[2] * d - s[3] * phase) * (1.0 - 0.35 * Math.Min(1.0, d));
            }
            return h / 1.5;
        }

        private static void Gradient(double t, out double r, out double g, out double b)
        {
            // deep -> mid -> light
            double[] deep = { 8, 52, 105 }, mid = { 28, 150, 205 }, light = { 175, 235, 255 };
            if (t < 0.55) Lerp(deep, mid, t / 0.55, out r, out g, out b);
            else Lerp(mid, light, (t - 0.55) / 0.45, out r, out g, out b);
        }

        private static void Lerp(double[] a, double[] b, double f, out double r, out double g, out double bl)
        {
            r = a[0] + (b[0] - a[0]) * f; g = a[1] + (b[1] - a[1]) * f; bl = a[2] + (b[2] - a[2]) * f;
        }

        private static double SmoothStep(double edge0, double edge1, double x)
        {
            double t = Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3.0 - 2.0 * t);
        }

        private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
        private static byte ToByte(double v) => (byte)Math.Max(0, Math.Min(255, Math.Round(v)));
    }
}

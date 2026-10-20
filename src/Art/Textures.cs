using System;

namespace PortalHeimSG1.Art
{
    /// <summary>The two stone textures the gate uses. Planar-mapped: the texture spans -R..+R across the ring.</summary>
    public static class Textures
    {
        public const int Size = 1024;

        /// <summary>The rotating inner ring, covered in runes.</summary>
        public static Canvas InnerRing(float outerRadius, float ringIn, float ringOut, int runeCount)
        {
            var c = StoneBase(outerRadius, ringIn, ringOut, 18, 112, 104, 92);
            float k = Size / (2f * outerRadius);
            Runes.PaintRing(c, Size / 2f, Size / 2f, ringIn * k, ringOut * k, runeCount, 3);
            return c;
        }

        /// <summary>The fixed outer ring: plain carved stone with a thin rim line.</summary>
        public static Canvas OuterRing(float outerRadius, float ringIn, float ringOut)
        {
            var c = StoneBase(outerRadius, ringIn, ringOut, 14, 96, 90, 80);
            float k = Size / (2f * outerRadius);
            float mid = (ringIn + ringOut) * 0.5f * k;
            // Two shallow grooves running around the ring, inside and outside the chevron line.
            Circle(c, mid - (ringOut - ringIn) * 0.28f * k, 3f, 54, 50, 44);
            Circle(c, mid + (ringOut - ringIn) * 0.28f * k, 3f, 54, 50, 44);
            return c;
        }

        private static Canvas StoneBase(float outerRadius, float ringIn, float ringOut, float speckle, byte r, byte g, byte b)
        {
            var c = new Canvas(Size, Size);
            c.Fill(r, g, b, 255);
            c.Speckle(7, speckle);
            return c;
        }

        private static void Circle(Canvas c, float radiusPx, float width, byte r, byte g, byte b)
        {
            const int steps = 720;
            float cx = c.Width / 2f, cy = c.Height / 2f;
            for (int i = 0; i < steps; i++)
            {
                double a0 = 2.0 * Math.PI * i / steps, a1 = 2.0 * Math.PI * (i + 1) / steps;
                c.Line(cx + (float)Math.Cos(a0) * radiusPx, cy + (float)Math.Sin(a0) * radiusPx,
                       cx + (float)Math.Cos(a1) * radiusPx, cy + (float)Math.Sin(a1) * radiusPx, width, r, g, b);
            }
        }
    }
}

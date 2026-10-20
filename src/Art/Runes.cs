using System;

namespace PortalHeimSG1.Art
{
    /// <summary>
    /// The 24 Elder Futhark runes as simple stroke shapes, and a painter that lays them around a ring.
    /// (Norse runes, not the show's glyphs: Stargate-inspired, not a copy.)
    /// Each rune is drawn in a box 1 wide by 2 tall, y up, as polylines.
    /// </summary>
    public static class Runes
    {
        // Fehu, Uruz, Thurisaz, Ansuz, Raido, Kenaz, Gebo, Wunjo, Hagalaz, Nauthiz, Isa, Jera,
        // Eihwaz, Perthro, Algiz, Sowilo, Tiwaz, Berkanan, Ehwaz, Mannaz, Laguz, Ingwaz, Dagaz, Othala
        private static readonly float[][][] Glyphs =
        {
            P(L(.3f,0, .3f,2), L(.3f,1.4f, .8f,1.9f), L(.3f,.8f, .8f,1.3f)),
            P(L(.15f,0, .15f,2, .8f,1.3f, .8f,0)),
            P(L(.2f,0, .2f,2), L(.2f,1.5f, .8f,1f, .2f,.5f)),
            P(L(.2f,0, .2f,2), L(.2f,1.8f, .8f,1.3f), L(.2f,1.2f, .8f,.7f)),
            P(L(.2f,0, .2f,2, .8f,1.5f, .2f,1f), L(.2f,1f, .8f,0)),
            P(L(.8f,1.7f, .2f,1f, .8f,.3f)),
            P(L(.15f,.1f, .85f,1.9f), L(.15f,1.9f, .85f,.1f)),
            P(L(.2f,0, .2f,2, .8f,1.5f, .2f,1f)),
            P(L(.15f,0, .15f,2), L(.85f,0, .85f,2), L(.15f,1.3f, .85f,.7f)),
            P(L(.5f,0, .5f,2), L(.15f,1.3f, .85f,.7f)),
            P(L(.5f,0, .5f,2)),
            P(L(.55f,1.95f, .2f,1.55f, .55f,1.15f), L(.45f,.85f, .8f,.45f, .45f,.05f)),
            P(L(.5f,0, .5f,2), L(.5f,2, .85f,1.7f), L(.5f,0, .15f,.3f)),
            P(L(.2f,0, .2f,2, .8f,1.6f, .8f,.6f, .2f,0)),
            P(L(.5f,0, .5f,2), L(.5f,1f, .15f,2), L(.5f,1f, .85f,2)),
            P(L(.7f,2, .3f,1.35f, .7f,.65f, .3f,0)),
            P(L(.5f,0, .5f,2), L(.5f,2, .15f,1.5f), L(.5f,2, .85f,1.5f)),
            P(L(.2f,0, .2f,2), L(.2f,2, .8f,1.5f, .2f,1f), L(.2f,1f, .8f,.5f, .2f,0)),
            P(L(.15f,0, .15f,2), L(.85f,0, .85f,2), L(.15f,2, .5f,1.4f, .85f,2)),
            P(L(.15f,0, .15f,2), L(.85f,0, .85f,2), L(.15f,2, .85f,1f), L(.85f,2, .15f,1f)),
            P(L(.3f,0, .3f,2), L(.3f,2, .75f,1.4f)),
            P(L(.5f,2, .9f,1f, .5f,0, .1f,1f, .5f,2)),
            P(L(.15f,0, .15f,2, .85f,0, .85f,2, .15f,0)),
            P(L(.2f,0, .5f,.8f, .8f,0), L(.5f,2, .15f,1.4f, .5f,.8f, .85f,1.4f, .5f,2)),
        };

        public static int GlyphCount => Glyphs.Length;

        /// <summary>Lays all runes out in a row (for checking each shape while developing).</summary>
        public static void PaintSheet(Canvas canvas)
        {
            float cell = canvas.Width / (float)Glyphs.Length;
            float scale = cell * 0.3f;
            for (int i = 0; i < Glyphs.Length; i++)
            {
                float ox = cell * (i + 0.5f), oy = canvas.Height / 2f;
                foreach (var line in Glyphs[i])
                    for (int k = 0; k + 3 < line.Length; k += 2)
                        canvas.Line(ox + (line[k] - 0.5f) * scale * 1.2f, oy + (line[k + 1] - 1f) * scale,
                                    ox + (line[k + 2] - 0.5f) * scale * 1.2f, oy + (line[k + 3] - 1f) * scale, scale * 0.28f, 42, 38, 34);
            }
        }

        private static float[] L(params float[] xy) => xy;
        private static float[][] P(params float[][] lines) => lines;

        /// <summary>
        /// Paints <paramref name="count"/> runes around a ring on <paramref name="canvas"/>, each standing
        /// radially (bottom toward the centre), as engraved grooves with a faint highlight.
        /// </summary>
        public static void PaintRing(Canvas canvas, float cx, float cy, float radiusIn, float radiusOut, int count, int seed)
        {
            float mid = (radiusIn + radiusOut) * 0.5f;
            float height = (radiusOut - radiusIn) * 0.72f;   // rune height along the radius
            float cell = 2f * (float)Math.PI * mid / count;    // room along the circumference
            float scale = Math.Min(height / 2f, cell * 0.42f); // keep glyphs from touching their neighbours
            float stroke = Math.Max(2.5f, scale * 0.3f);

            for (int i = 0; i < count; i++)
            {
                var glyph = Glyphs[(i + seed) % Glyphs.Length];
                double theta = 2.0 * Math.PI * i / count;
                float ux = (float)Math.Cos(theta), uy = (float)Math.Sin(theta);    // outward
                float tx = -uy, ty = ux;                                           // along the ring

                foreach (var line in glyph)
                {
                    for (int k = 0; k + 3 < line.Length; k += 2)
                    {
                        Map(line[k], line[k + 1], out float ax, out float ay);
                        Map(line[k + 2], line[k + 3], out float bx, out float by);
                        // Highlight below-right, then the groove on top: reads as engraved stone.
                        canvas.Line(ax + 1.5f, ay - 1.5f, bx + 1.5f, by - 1.5f, stroke, 190, 184, 170, 0.55f);
                        canvas.Line(ax, ay, bx, by, stroke, 42, 38, 34);
                    }
                }

                void Map(float gx, float gy, out float x, out float y)
                {
                    float along = (gx - 0.5f) * scale * 1.2f;   // sideways
                    float radial = (gy - 1f) * scale;            // up the glyph = outward
                    x = cx + ux * (mid + radial) + tx * along;
                    y = cy + uy * (mid + radial) + ty * along;
                }
            }
        }
    }
}

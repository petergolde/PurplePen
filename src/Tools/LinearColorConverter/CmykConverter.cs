using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurplePen
{
    partial class CmykConverter
    {
        private readonly int SAMPLESIZE;

        private struct RGB
        {
            public float R, G, B;
            public RGB(float r, float g, float b)
            {
                this.R = r;
                this.G = g;
                this.B = b;
            }
        }

        public CmykConverter()
        {
            SAMPLESIZE = samples.GetLength(0);
        }

        // Convert CMYK to RGB colors. Uses quadrilinear interpretation from a pre-computed set of lookup values,
        // which are stored in another file.
        public void Convert(float c, float m, float y, float k, out float red, out float green, out float blue)
        {
            // See https://en.wikipedia.org/wiki/Trilinear_interpolation; extended to 4 dimensions.

            int iLow = (int)Math.Floor(c * (SAMPLESIZE - 1));
            int iHigh = iLow < SAMPLESIZE - 1 ? iLow + 1 : iLow;
            float iFrac = c * (SAMPLESIZE - 1) - iLow;
            int jLow = (int)Math.Floor(m * (SAMPLESIZE - 1));
            int jHigh = jLow < SAMPLESIZE - 1 ? jLow + 1 : jLow;
            float jFrac = m * (SAMPLESIZE - 1) - jLow;
            int kLow = (int)Math.Floor(y * (SAMPLESIZE - 1));
            int kHigh = kLow < SAMPLESIZE - 1 ? kLow + 1 : kLow;
            float kFrac = y * (SAMPLESIZE - 1) - kLow;
            int lLow = (int)Math.Floor(k * (SAMPLESIZE - 1));
            int lHigh = lLow < SAMPLESIZE - 1 ? lLow + 1 : lLow;
            float lFrac = k * (SAMPLESIZE - 1) - lLow;

            RGB rgb000 = Interp(RgbSample(iLow, jLow, kLow, lLow), RgbSample(iHigh, jLow, kLow, lLow), iFrac);
            RGB rgb001 = Interp(RgbSample(iLow, jLow, kLow, lHigh), RgbSample(iHigh, jLow, kLow, lHigh), iFrac);
            RGB rgb010 = Interp(RgbSample(iLow, jLow, kHigh, lLow), RgbSample(iHigh, jLow, kHigh, lLow), iFrac);
            RGB rgb011 = Interp(RgbSample(iLow, jLow, kHigh, lHigh), RgbSample(iHigh, jLow, kHigh, lHigh), iFrac);
            RGB rgb100 = Interp(RgbSample(iLow, jHigh, kLow, lLow), RgbSample(iHigh, jHigh, kLow, lLow), iFrac);
            RGB rgb101 = Interp(RgbSample(iLow, jHigh, kLow, lHigh), RgbSample(iHigh, jHigh, kLow, lHigh), iFrac);
            RGB rgb110 = Interp(RgbSample(iLow, jHigh, kHigh, lLow), RgbSample(iHigh, jHigh, kHigh, lLow), iFrac);
            RGB rgb111 = Interp(RgbSample(iLow, jHigh, kHigh, lHigh), RgbSample(iHigh, jHigh, kHigh, lHigh), iFrac);

            RGB rgb00 = Interp(rgb000, rgb100, jFrac);
            RGB rgb01 = Interp(rgb001, rgb101, jFrac);
            RGB rgb10 = Interp(rgb010, rgb110, jFrac);
            RGB rgb11 = Interp(rgb011, rgb111, jFrac);

            RGB rgb0 = Interp(rgb00, rgb10, kFrac);
            RGB rgb1 = Interp(rgb01, rgb11, kFrac);

            RGB rgb = Interp(rgb0, rgb1, lFrac);

            red = rgb.R;
            green = rgb.G;
            blue = rgb.B;
        }

        private RGB RgbSample(int i, int j, int k, int l)
        {
            return new RGB(samples[i, j, k, l, 0], samples[i, j, k, l, 1], samples[i, j, k, l, 2]);
        }

        private RGB Interp(RGB rgbLow, RGB rgbHigh, float frac)
        {
            return new RGB(rgbLow.R * (1 - frac) + rgbHigh.R * frac,
                           rgbLow.G * (1 - frac) + rgbHigh.G * frac,
                           rgbLow.B * (1 - frac) + rgbHigh.B * frac);
        }
    }
}

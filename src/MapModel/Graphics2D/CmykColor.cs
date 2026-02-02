// Which CMYK -> RGB color conversion algorithm to use?
#define OCADRGBCMYK

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace PurplePen.Graphics2D
{
    /// <summary>
    /// A CmykColor represents a color in CMYK + Alpha form. It can store colors in either RGB
    /// or CMYK (with alpha for each) without loss. Storing a CMYK color in RGB form loses
    /// information, but the reverse is possible without loss.
    /// </summary>
    public class CmykColor
    {
        private readonly float cyan, magenta, yellow, black, alpha;

        private CmykColor(float cyan, float magenta, float yellow, float black, float alpha)
        {
            this.cyan = cyan;
            this.magenta = magenta;
            this.yellow = yellow;
            this.black = black;
            this.alpha = alpha;
        }

        public static CmykColor FromCmyk(float cyan, float magenta, float yellow, float black)
        {
            return new CmykColor(cyan, magenta, yellow, black, 1.0F);
        }

        public static CmykColor FromCmyka(float cyan, float magenta, float yellow, float black, float alpha)
        {
            return new CmykColor(cyan, magenta, yellow, black, alpha);
        }

        public static CmykColor FromRgb(float red, float green, float blue)
        {
            float cyan, magenta, yellow, black;

            ColorConverter.RgbToCmyk(red, green, blue, out cyan, out magenta, out yellow, out black);
            return new CmykColor(cyan, magenta, yellow, black, 1.0F);
        }

        public static CmykColor FromRgba(float red, float green, float blue, float alpha)
        {
            float cyan, magenta, yellow, black;

            ColorConverter.RgbToCmyk(red, green, blue, out cyan, out magenta, out yellow, out black);
            return new CmykColor(cyan, magenta, yellow, black, alpha);
        }

        public static CmykColor FromColor(Color color)
        {
            float red, green, blue, alpha;
            red = color.R / 255F;
            green = color.G / 255F;
            blue = color.B / 255F;
            alpha = color.A / 255F;
            return FromRgba(red, green, blue, alpha);
        }

        public float Cyan
        {
            get { return cyan; }
        }

        public float Magenta
        {
            get { return magenta; }
        }

        public float Yellow
        {
            get { return yellow; }
        }

        public float Black
        {
            get { return black; }
        }

        public float Alpha
        {
            get { return alpha; }
        }

        public float Red
        {
            get
            {
                float red, green, blue;
                ColorConverter.CmykToRgb(cyan, magenta, yellow, black, out red, out green, out blue);
                return red;
            }
        }

        public float Green
        {
            get
            {
                float red, green, blue;
                ColorConverter.CmykToRgb(cyan, magenta, yellow, black, out red, out green, out blue);
                return green;
            }
        }

        public float Blue
        {
            get
            {
                float red, green, blue;
                ColorConverter.CmykToRgb(cyan, magenta, yellow, black, out red, out green, out blue);
                return blue;
            }
        }

        public override bool Equals(object obj)
        {
            CmykColor other = obj as CmykColor;
            if (other != null) {
                return (other.cyan == this.cyan &&
                        other.magenta == this.magenta &&
                        other.yellow == this.yellow &&
                        other.black == this.black &&
                        other.alpha == this.alpha);
            }
            else {
                return false;
            }
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + cyan.GetHashCode();
            hash = hash * 31 + magenta.GetHashCode();
            hash = hash * 31 + yellow.GetHashCode();
            hash = hash * 31 + black.GetHashCode();
            hash = hash * 31 + alpha.GetHashCode();
            return hash;
        }
    }

    public static class ColorConverter
    {
        public static Color CmykaToColor(float cyan, float magenta, float yellow, float black, float alpha)
        {
            float red, green, blue;
            byte r, g, b, a;
            CmykToRgb(cyan, magenta, yellow, black, out red, out green, out blue);
            r = (byte)Math.Round((float)(red * 255F));
            g = (byte)Math.Round((float)(green * 255F));
            b = (byte)Math.Round((float)(blue * 255F));
            a = (byte)Math.Round((float)(alpha * 255F));
            return Color.FromArgb(a, r, g, b);
        }

        public static Color CmykToColor(float cyan, float magenta, float yellow, float black)
        {
            return CmykaToColor(cyan, magenta, yellow, black, 1.0F);
        }

        public static Color ToColor(CmykColor cmykColor)
        {
            return CmykaToColor(cmykColor.Cyan, cmykColor.Magenta, cmykColor.Yellow, cmykColor.Black, cmykColor.Alpha);
        }

        public static void CmykToRgb(float cyan, float magenta, float yellow, float black, out float red, out float green, out float blue)
        {
#if OCADRGBCMYK
            red = (float)Math.Max(0.0F, 1.0F - (cyan + black));
            green = (float)Math.Max(0.0F, 1.0F - (magenta + black));
            blue = (float)Math.Max(0.0F, 1.0F - (yellow + black));
#else
            red = Math.Max(0F, 1 - (cyan * (1 - black) + black));
            green = Math.Max(0F, 1 - (magenta * (1 - black) + black));
            blue = Math.Max(0F, 1 - (yellow * (1 - black) + black));
#endif
        }

        public static void RgbToCmyk(float red, float green, float blue, out float cyan, out float magenta, out float yellow, out float black)
        {
#if OCADRGBCMYK
            cyan = 1.0F - red;
            magenta = 1.0F - green;
            yellow = 1.0F - blue;

            black = Math.Min(cyan, Math.Min(magenta, yellow));
            cyan -= black;
            magenta -= black;
            yellow -= black;
#else
            cyan = 1.0F - red;
            magenta = 1.0F - green;
            yellow = 1.0F - blue;

            black = Math.Min(cyan, Math.Min(magenta, yellow));
            if (black > 0.99)
            {
                cyan = magenta = yellow = 0;
            }
            else
            {
                cyan = (cyan - black) / (1 - black);
                magenta = (magenta - black) / (1 - black);
                yellow = (yellow - black) / (1 - black);
            }
#endif
        }

        public static void CmykToHsv(float cyan, float magenta, float yellow, float black, out float h, out float s, out float v)
        {
            float red, green, blue;
            CmykToRgb(cyan, magenta, yellow, black, out red, out green, out blue);
            RgbToHsv(red, green, blue, out h, out s, out v);
        }

        public static void HsvToCmyk(float h, float s, float b, out float cyan, out float magenta, out float yellow, out float black)
        {
            float red, green, blue;
            HsvToRgb(h, s, b, out red, out green, out blue);
            RgbToCmyk(red, green, blue, out cyan, out magenta, out yellow, out black);
        }

        public static void RgbToHsv(float r, float g, float b, out float h, out float s, out float v)
        {
            float min, max, delta;
            min = Math.Min(r, Math.Min(g, b));
            max = Math.Max(r, Math.Max(g, b));

            v = max;				// v
            delta = max - min;
            if (max != 0)
                s = delta / max;		// s
            else {
                // r = g = b = 0		// s = 0, v is undefined
                s = 0;
                h = 0;
                return;
            }
            if (s == 0)
                h = 0;			// no s, so h is undefined
            else {
                if (r == max)
                    h = (g - b) / delta;		// between yellow & magenta
                else if (g == max)
                    h = 2 + (b - r) / delta;	// between cyan & yellow
                else
                    h = 4 + (r - g) / delta;	// between magenta & cyan
                h /= 6;				// to get 0-1.
                if (h < 0)
                    h += 1F;
            }
        }

        public static void HsvToRgb(float h, float s, float v, out float r, out float g, out float b)
        {
            int i;
            float f, p, q, t;
            if (s == 0) {
                // achromatic (grey)
                r = g = b = v;
                return;
            }
            if (h >= 1)
                h -= 1;
            h *= 6;			// sector 0 to 5
            i = (int)Math.Floor(h);
            f = h - i;			// factorial part of h
            p = v * (1 - s);
            q = v * (1 - s * f);
            t = v * (1 - s * (1 - f));
            switch (i) {
                case 0:
                    r = v;
                    g = t;
                    b = p;
                    break;
                case 1:
                    r = q;
                    g = v;
                    b = p;
                    break;
                case 2:
                    r = p;
                    g = v;
                    b = t;
                    break;
                case 3:
                    r = p;
                    g = q;
                    b = v;
                    break;
                case 4:
                    r = t;
                    g = p;
                    b = v;
                    break;
                default:		// case 5:
                    r = v;
                    g = p;
                    b = q;
                    break;
            }
        }
    }
}

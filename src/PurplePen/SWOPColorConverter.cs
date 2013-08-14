using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PurplePen.MapModel;
using PurplePen.Graphics2D;
using SWM = System.Windows.Media;
using SD = System.Drawing;

namespace PurplePen
{
    class SwopColorConverter: GDIPlus_ColorConverter
    {
        public readonly static string SwopFileName;
        public readonly static Uri SwopUri;
        private static Dictionary<CmykColor, SD.Color> cmykToColor = new Dictionary<CmykColor,SD.Color>();

        static SwopColorConverter()
        {
            SwopFileName = Util.GetFileInAppDirectory("USWebCoatedSWOP.icc");
            SwopUri = new Uri(SwopFileName);
        }

        public static SD.Color CmykToRgbColor(CmykColor cmykColor)
        {
            SD.Color result;

            if (!cmykToColor.TryGetValue(cmykColor, out result)) {
                float[] colorValues = new float[4] { cmykColor.Cyan, cmykColor.Magenta, cmykColor.Yellow, cmykColor.Black };
                SWM.Color color = SWM.Color.FromValues(colorValues, SwopUri);
                result = SD.Color.FromArgb(color.R, color.G, color.B);
                lock (cmykToColor) {
                    cmykToColor[cmykColor] = result;
                }
            }

            return result;
        }

        public override SD.Color ToColor(CmykColor cmykColor)
        {
            return CmykToRgbColor(cmykColor);
        }
    }

    class WPFSwopColorConverter : WPF_ColorConverter
    {
        private Dictionary<CmykColor, SWM.Color> cmykToColor = new Dictionary<CmykColor, SWM.Color>();

        public override SWM.Color ToColor(CmykColor cmykColor)
        {
            SWM.Color result;

            if (!cmykToColor.TryGetValue(cmykColor, out result)) {
                float[] colorValues = new float[4] { cmykColor.Cyan, cmykColor.Magenta, cmykColor.Yellow, cmykColor.Black };
                result = SWM.Color.FromValues(colorValues, SwopColorConverter.SwopUri);
                cmykToColor[cmykColor] = result;
            }

            return result;
        }
    }
}

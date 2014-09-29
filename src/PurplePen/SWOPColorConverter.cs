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

            if (cmykColor.Cyan == 0 && cmykColor.Magenta == 0 && cmykColor.Yellow == 0 && cmykColor.Black == 0) {
                // The default mapping doesn't quite map white to pure white.
                if (cmykColor.Alpha == 1)
                    return SD.Color.White;
                else
                    return SD.Color.FromArgb((byte) Math.Round(cmykColor.Alpha * 255), SD.Color.White);
            }

            if (!cmykToColor.TryGetValue(cmykColor, out result)) {
                float[] colorValues = new float[4] { cmykColor.Cyan, cmykColor.Magenta, cmykColor.Yellow, cmykColor.Black };
                SWM.Color color = SWM.Color.FromValues(colorValues, SwopUri);
                result = SD.Color.FromArgb((byte) Math.Round(cmykColor.Alpha * 255), color.R, color.G, color.B);
                lock (cmykToColor) {
                    cmykToColor[cmykColor] = result;
                }
            }

            return result;
        }

        public override SD.Color ToColor(CmykColor cmykColor)
        {
            try {
                return CmykToRgbColor(cmykColor);
            }
            catch (Exception) {
                // In some cases, a weirdly installed .NET framework will cause an exception here.
                System.Windows.Forms.MessageBox.Show(MiscText.BadDotNetFramework, MiscText.AppTitle);
                Environment.Exit(1);
                return new SD.Color();
            }
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

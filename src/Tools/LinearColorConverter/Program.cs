using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LinearColorConverter
{
    class Program
    {
        static ColorModel colorModel = new ColorModel();

        static void Main(string[] args)
        {
            Console.WriteLine("Populating...");

            colorModel.Populate();
            
            Test(0, 0, 0, 0);
            Test(1, 1, 1, 1);
            Test(1, 0, 0, 0);
            Test(0, 1, 0, 0);
            Test(0, 0, 1, 0);
            Test(0, 0, 0, 1);
            Console.WriteLine();
            Test(0.0F, 0.4F, 0.6F, 0.8F);
            Test(0.05F, 0.4F, 0.6F, 0.8F);
            Test(0.1F, 0.4F, 0.6F, 0.8F);
            Test(0.15F, 0.4F, 0.6F, 0.8F);
            Test(0.2F, 0.4F, 0.6F, 0.8F);
            Console.WriteLine();
            Test(0.2F, 0.4F, 0.6F, 0.8F);
            Test(0.2F, 0.45F, 0.6F, 0.8F);
            Test(0.2F, 0.5F, 0.6F, 0.8F);
            Test(0.2F, 0.55F, 0.6F, 0.8F);
            Test(0.2F, 0.6F, 0.6F, 0.8F);
            Console.WriteLine();
            Test(0.2F, 0.4F, 0.6F, 0.8F);
            Test(0.2F, 0.4F, 0.65F, 0.8F);
            Test(0.2F, 0.4F, 0.7F, 0.8F);
            Test(0.2F, 0.4F, 0.75F, 0.8F);
            Test(0.2F, 0.4F, 0.8F, 0.8F);
            Console.WriteLine();
            Test(0.2F, 0.4F, 0.6F, 0.8F);
            Test(0.2F, 0.4F, 0.6F, 0.85F);
            Test(0.2F, 0.4F, 0.6F, 0.9F);
            Test(0.2F, 0.4F, 0.6F, 0.95F);
            Test(0.2F, 0.4F, 0.6F, 1.0F);
            
            
            double maxDiff = 0;
            float maxC = 0, maxM = 0, maxY = 0, maxK = 0;

            Console.WriteLine("Testing...");

            for (int i = 0; i < 10000; ++i) {
                if (i % 1000 == 0) {
                    Console.Write($"{i}... ");
                }
                Random rand = new Random();
                float c = (float)rand.NextDouble();
                float m = (float)rand.NextDouble();
                float y = (float)rand.NextDouble();
                float k = (float)rand.NextDouble();
                RGB correct = colorModel.ConvertUsingICC(c, y, m, k);
                RGB interp = colorModel.ConvertUsingInterpolation(c, y, m, k);
                double diff = Diff(correct, interp);

                if (diff > maxDiff) {
                    maxC = c; maxM = m; maxY = y; maxK = k;
                }
            }

            Console.WriteLine();
            Test(maxC, maxM, maxY, maxK);
            

            colorModel.OutputSamples("CmykConverterSamples.cs");
        }

        static void Test(float c, float y, float m, float k)
        {
            RGB correct = colorModel.ConvertUsingICC(c, y, m, k);
            RGB interp = colorModel.ConvertUsingInterpolation(c, y, m, k);

            Console.WriteLine($"Converting [C={c:F3},Y={y:F3},M={m:F3},K={k:F3}]: Correct is (R:{correct.R:F3},G:{correct.G:F3},B:{correct.B:F3})  Interpolated is (R:{interp.R:F3},G:{interp.G:F3},B:{interp.B:F3})");
        }

        static double Diff(RGB rgb1, RGB rgb2)
        {
            return Math.Sqrt(((rgb1.B - rgb2.B) * (rgb1.B - rgb2.B)) + ((rgb1.G - rgb2.G) * (rgb1.G - rgb2.G)) + ((rgb1.R - rgb2.R) * (rgb1.R - rgb2.R)));
        }
    }

    struct RGB
    {
        public float R, G, B;
        public RGB(float r, float g, float b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }
    }

    class ColorModel
    {
        public const int SAMPLESIZE = 8;  // Was 11.
        public RGB[,,,] samples = new RGB[SAMPLESIZE, SAMPLESIZE, SAMPLESIZE, SAMPLESIZE];

        Uri SwopUri;

        public void Populate()
        {
            string swopFileName = GetFileInAppDirectory("USWebCoatedSWOP.icc");
            SwopUri = new Uri(swopFileName);
            float delta = 1.0F / (SAMPLESIZE - 1);

            for (int i = 0; i < SAMPLESIZE; ++i) {
                float cyan = i * delta;
                for (int j = 0; j < SAMPLESIZE; ++j) {
                    float mag = j * delta;
                    for (int k = 0; k < SAMPLESIZE; ++k) {
                        float yel = k * delta;
                        for (int l = 0; l < SAMPLESIZE; ++l) {
                            float blk = l * delta;

                            samples[i, j, k, l] = ConvertUsingICC(cyan, mag, yel, blk);
                        }
                    }
                }
            }
        }

        public RGB ConvertUsingICC(float c, float m, float y, float k)
        {
            float[] colorValues = { c, m, y, k };
            Color color = Color.FromValues(colorValues, SwopUri);
            return new RGB(color.ScR, color.ScG, color.ScB);
        }

        public RGB ConvertUsingInterpolation(float c, float m, float y, float k)
        {
            // https://en.wikipedia.org/wiki/Trilinear_interpolation

            int iLow = (int) Math.Floor(c * (SAMPLESIZE - 1));
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

            RGB rgb000 = Interp(samples[iLow, jLow, kLow, lLow], samples[iHigh, jLow, kLow, lLow], iFrac);
            RGB rgb001 = Interp(samples[iLow, jLow, kLow, lHigh], samples[iHigh, jLow, kLow, lHigh], iFrac);
            RGB rgb010 = Interp(samples[iLow, jLow, kHigh, lLow], samples[iHigh, jLow, kHigh, lLow], iFrac);
            RGB rgb011 = Interp(samples[iLow, jLow, kHigh, lHigh], samples[iHigh, jLow, kHigh, lHigh], iFrac);
            RGB rgb100 = Interp(samples[iLow, jHigh, kLow, lLow], samples[iHigh, jHigh, kLow, lLow], iFrac);
            RGB rgb101 = Interp(samples[iLow, jHigh, kLow, lHigh], samples[iHigh, jHigh, kLow, lHigh], iFrac);
            RGB rgb110 = Interp(samples[iLow, jHigh, kHigh, lLow], samples[iHigh, jHigh, kHigh, lLow], iFrac);
            RGB rgb111 = Interp(samples[iLow, jHigh, kHigh, lHigh], samples[iHigh, jHigh, kHigh, lHigh], iFrac);

            RGB rgb00 = Interp(rgb000, rgb100, jFrac);
            RGB rgb01 = Interp(rgb001, rgb101, jFrac);
            RGB rgb10 = Interp(rgb010, rgb110, jFrac);
            RGB rgb11 = Interp(rgb011, rgb111, jFrac);

            RGB rgb0 = Interp(rgb00, rgb10, kFrac);
            RGB rgb1 = Interp(rgb01, rgb11, kFrac);

            RGB rgb = Interp(rgb0, rgb1, lFrac);

            return rgb;
        }

        public void OutputSamples(string filename)
        {
            using (TextWriter writer = new StreamWriter(filename)) {
                writer.WriteLine("// Lookup values for linear interpolation of CMYK -> RGB conversion.");
                writer.WriteLine("// Automatically created by the LinearColorConverter tool at {0}", DateTime.Now);
                writer.WriteLine();
                writer.WriteLine("namespace PurplePen {");
                writer.WriteLine("    partial class CmykConverter {");
                writer.WriteLine("        private float[,,,,] samples = {");

                for (int i = 0; i < SAMPLESIZE; ++i) {
                    writer.WriteLine("            {");
                    for (int j = 0; j < SAMPLESIZE; ++j) {
                        writer.WriteLine("                {");
                        for (int k = 0; k < SAMPLESIZE; ++k) {
                            writer.Write("                    {");
                            for (int l = 0; l < SAMPLESIZE; ++l) {
                                writer.Write("{{{0:R}F,{1:R}F,{2:R}F}}, ", samples[i, j, k, l].R, samples[i, j, k, l].G, samples[i, j, k, l].B);
                            }
                            writer.WriteLine("},");
                        }
                        writer.WriteLine("                },");
                    }
                    writer.WriteLine("            },");
                }

                writer.WriteLine("        };");
                writer.WriteLine("    }");
                writer.WriteLine("}");
            }
        }

        private RGB Interp(RGB rgbLow, RGB rgbHigh, float frac)
        {
            return new RGB(rgbLow.R * (1 - frac) + rgbHigh.R * frac,
                rgbLow.G * (1 - frac) + rgbHigh.G * frac,
                rgbLow.B * (1 - frac) + rgbHigh.B * frac);
        }

        private string GetFileInAppDirectory(string filename)
        {
            // Using Application.StartupPath would be
            // simpler and probably faster, but doesn't work with NUnit.
            string codebase = this.GetType().Assembly.CodeBase;
            Uri uri = new Uri(codebase);
            string appPath = Path.GetDirectoryName(uri.LocalPath);

            // Create the core objects needed for the application to run.
            return Path.Combine(appPath, filename);
        }
    }
}

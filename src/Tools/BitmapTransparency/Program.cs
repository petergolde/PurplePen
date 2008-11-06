using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace BitmapTransparency
{
    class Program
    {
        static Color newColor = Color.Magenta;

        static void TranslateBitmap(string inputFile, string outputFile)
        {
            Bitmap inputBitmap = (Bitmap) Image.FromFile(inputFile);
            Bitmap outputBitmap = new Bitmap(inputBitmap.Width, inputBitmap.Height, PixelFormat.Format32bppArgb);

            for (int x = 0; x < inputBitmap.Width; ++x) {
                for (int y = 0; y < inputBitmap.Height; ++y) {
                    Color pixel = inputBitmap.GetPixel(x, y);
                    int transparency = 255 - pixel.R;
                    Color newPixel = Color.FromArgb(transparency, newColor);
                    outputBitmap.SetPixel(x, y, newPixel);
                }
            }

            outputBitmap.Save(outputFile, ImageFormat.Png);
            inputBitmap.Dispose();
            outputBitmap.Dispose();
        }

        static void Main(string[] args)
        {
            string inputFile = @"C:\Users\Peter\Documents\Purplepen\branches\textspecial\src\PurplePen\Images\TextBW.png";
            string outputFile = @"C:\Users\Peter\Documents\Purplepen\branches\textspecial\src\PurplePen\Images\TextTransparent.png";

            TranslateBitmap(inputFile, outputFile);
        }
    }
}

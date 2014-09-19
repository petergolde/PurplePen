using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace BitmapTransparency
{
    class Program
    {
        static void TranslateBitmap(string inputFile, string outputFile)
        {
            Bitmap inputBitmap = (Bitmap) Image.FromFile(inputFile);
            Bitmap outputBitmap = new Bitmap(24, 24, PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(outputBitmap)) {
                g.Clear(Color.White);

                g.DrawImage(inputBitmap, new Rectangle(4, 4, 16, 16));
            }

            outputBitmap.Save(outputFile, ImageFormat.Png);
            inputBitmap.Dispose();
            outputBitmap.Dispose();
        }

        static void Main(string[] args)
        {
            TranslateBitmap(args[0], args[1]);
        }
    }
}

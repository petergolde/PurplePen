using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PurplePen
{
    // Class that does faster bitmap drawing to an HDC.
    // Does not handle transparency!
    static class FastBitmapPaint
    {
        static class NativeMethods
        {
            [DllImport("gdi32")]
            public extern static int SetDIBitsToDevice(IntPtr hDC, int xDest, int yDest, int dwWidth, int dwHeight, int XSrc, int YSrc, int uStartScan, int cScanLines, IntPtr lpvBits, ref BITMAPINFO lpbmi, uint fuColorUse);

            [StructLayout(LayoutKind.Sequential)]
            public struct BITMAPINFOHEADER
            {
                public int bihSize;
                public int bihWidth;
                public int bihHeight;
                public short bihPlanes;
                public short bihBitCount;
                public int bihCompression;
                public int bihSizeImage;
                public double bihXPelsPerMeter;
                public double bihClrUsed;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct BITMAPINFO
            {
                public BITMAPINFOHEADER biHeader;
                public int biColors;
            }
        }

        public static void PaintBitmap(Graphics graphics, Bitmap bitmap, Rectangle src, Point dest)
        {
            int bmWidth = bitmap.Width, bmHeight = bitmap.Height;
            PixelFormat pixelFormat = bitmap.PixelFormat;
            int bytesPerPixel;

            if (bmWidth == 0 || bmHeight == 0) {
                return;
            }

            bytesPerPixel = Image.GetPixelFormatSize(pixelFormat) / 8;

            BitmapData BD = bitmap.LockBits(new Rectangle(0, 0, bmWidth, bitmap.Height),
                                            ImageLockMode.ReadOnly,
                                            pixelFormat);
            IntPtr hdc = graphics.GetHdc();
            try {

                NativeMethods.BITMAPINFO bmInfo = new NativeMethods.BITMAPINFO {
                    biHeader =
                {
                    bihBitCount = (short) (bytesPerPixel * 8),
                    bihPlanes = 1,
                    bihSize = 40,
                    bihWidth = BD.Stride / bytesPerPixel,
                    bihHeight = -bmHeight,
                    bihSizeImage = BD.Stride * bmHeight
                }
                };

                NativeMethods.SetDIBitsToDevice(hdc, dest.X, dest.Y, src.Width, src.Height, src.X, bmHeight - src.Bottom, 0, bmHeight, BD.Scan0, ref bmInfo, 0);
            }
            finally {
                bitmap.UnlockBits(BD);
                graphics.ReleaseHdc();
            }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CrashReporterDotNET
{
    internal class CaptureScreenshot
    {
        public Image CaptureScreen()
        {
            return CaptureWindow(NativeMethods.GetDesktopWindow());
        }

        public Image CaptureWindow(IntPtr handle)
        {
            // get te hDC of the target window
            IntPtr hdcSrc = NativeMethods.GetWindowDC(handle);
            // get the size
            var windowRect = new NativeMethods.Rect();
            NativeMethods.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // create a device context we can copy to
            IntPtr hdcDest = NativeMethods.CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = NativeMethods.CreateCompatibleBitmap(hdcSrc, width, height);
            // select the bitmap object
            IntPtr hOld = NativeMethods.SelectObject(hdcDest, hBitmap);
            // bitblt over
            NativeMethods.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, NativeMethods.Srccopy);
            // restore selection
            NativeMethods.SelectObject(hdcDest, hOld);
            // clean up
            NativeMethods.DeleteDC(hdcDest);
            NativeMethods.ReleaseDC(handle, hdcSrc);
            // get a .NET image object for it
            Image img = Image.FromHbitmap(hBitmap);
            // free up the Bitmap object
            NativeMethods.DeleteObject(hBitmap);
            return img;
        }

        // Capture screenshot of a window
        public void CaptureWindowToFile(IntPtr handle, string filename, ImageFormat format)
        {
            Image img = CaptureWindow(handle);
            img.Save(filename, format);
        }

        // Capture desktop screenshot
        public void CaptureScreenToFile(string filename, ImageFormat format)
        {
            Image img = CaptureScreen();
            img.Save(filename, format);
        }

        private static class NativeMethods
        {
            public const int Srccopy = 0x00CC0020; // BitBlt dwRop parameter

            [DllImport("NativeMethods.dll")]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                                             int nWidth, int nHeight, IntPtr hObjectSource,
                                             int nXSrc, int nYSrc, int dwRop);

            [DllImport("NativeMethods.dll")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDc, int nWidth,
                                                               int nHeight);

            [DllImport("NativeMethods.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDc);

            [DllImport("NativeMethods.dll")]
            public static extern bool DeleteDC(IntPtr hDc);

            [DllImport("NativeMethods.dll")]
            public static extern bool DeleteObject(IntPtr hObject);

            [DllImport("NativeMethods.dll")]
            public static extern IntPtr SelectObject(IntPtr hDc, IntPtr hObject);

            [DllImport("NativeMethods.dll")]
            public static extern IntPtr GetDesktopWindow();

            [DllImport("NativeMethods.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);

            [DllImport("NativeMethods.dll")]
            public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

            [DllImport("NativeMethods.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

            [StructLayout(LayoutKind.Sequential)]
            public struct Rect
            {
                public readonly int left;
                public readonly int top;
                public readonly int right;
                public readonly int bottom;
            }
        }
    }
}
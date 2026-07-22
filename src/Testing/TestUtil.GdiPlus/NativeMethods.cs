using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TestingUtils
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool ScrollWindow(IntPtr hwnd, int dx, int dy, IntPtr lpRect, IntPtr lpClipRect);

    }
}

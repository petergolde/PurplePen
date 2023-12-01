using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PurplePen
{
    // This is a fix found here:
    //    https://github.com/dotnet/winforms/issues/4854#issuecomment-829050081
    // For forms showing up with the wrong DPI when different monitors
    // have different DPIs. It should not be necessary on .NET 7.
    public class DpiFixedForm: Form
    {
        internal const uint WM_DPICHANGED = 0x02E0;

        [DllImport("user32.dll")]
        internal static extern int GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, ref RECT lParam);

        internal static IntPtr MakeLParam(int lowWord, int highWord)
        {
            return (IntPtr)((highWord << 16) | (lowWord & 0xffff));
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            try {
                base.OnHandleCreated(e);

                int realDpi = GetDpiForWindow(this.Handle);
                double scaleFactor = (double)realDpi / (double)this.DeviceDpi;

                int newWidth = (int)(this.Size.Width * scaleFactor);
                int newHeight = (int)(this.Size.Height * scaleFactor);

                int dpiLeft = this.Location.X + ((this.Size.Width - newWidth) / 2);
                int dpiTop = this.Location.Y + ((this.Size.Height - newHeight) / 2); ;
                int dpiRight = dpiLeft + newWidth;
                int dpiBottom = dpiTop + newHeight;

                RECT rect = new RECT() { left = dpiLeft, top = dpiTop, right = dpiRight, bottom = dpiBottom };
                SendMessage(this.Handle, WM_DPICHANGED, MakeLParam(realDpi, realDpi), ref rect);
            }
            catch {
                // This doesn't work on Windows 7, because GetDpiForWindow is an API that doesn't exist on Windows 7.
                // Ignore all exceptions here anyway, because if this fails, we just get the wrong DPI.
            }

        }
    }
}

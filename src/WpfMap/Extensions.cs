using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Rect = System.Windows.Rect;
using RectangleF = System.Drawing.RectangleF;

namespace WpfMap
{
    static class Extensions
    {
        public static RectangleF ToRectangleF(this Rect rect) {
            return new RectangleF((float)rect.Left, (float)rect.Top, (float)rect.Width, (float)rect.Height);
        }

        public static Rect ToRect(this RectangleF rectF) {
            return new Rect(rectF.Left, rectF.Top, rectF.Width, rectF.Height);
        }
    }
}

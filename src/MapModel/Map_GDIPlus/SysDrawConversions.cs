using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PurplePen.MapModel
{
    public static class SysDrawConversions
    {
        public static System.Drawing.Drawing2D.Matrix ToSysDrawMatrix(this PurplePen.Graphics2D.Matrix matrix)
        {
            float[] elements = matrix.Elements;
            return new System.Drawing.Drawing2D.Matrix(
                elements[0], elements[1],
                elements[2], elements[3],
                elements[4], elements[5]);
        }

        public static PurplePen.Graphics2D.Matrix ToGraphics2DMatrix(this System.Drawing.Drawing2D.Matrix matrix)
        {
            float[] elements = matrix.Elements;
            return new PurplePen.Graphics2D.Matrix(
                elements[0], elements[2],
                elements[1], elements[3],
                elements[4], elements[5]);
        }

        public static System.Drawing.Imaging.ColorMatrix ToSysDrawColorMatrix(this ColorMatrix colorMatrix)
        {
            float[][] elements = colorMatrix.Elements;
            return new System.Drawing.Imaging.ColorMatrix(elements);
        }

        public static System.Drawing.Drawing2D.LineCap ToSysDrawLineCap(this PurplePen.Graphics2D.LineCapMode lineCap)
        {
            return (System.Drawing.Drawing2D.LineCap)(int)lineCap;
        }

        public static System.Drawing.Drawing2D.LineJoin ToSysDrawLineJoin(this PurplePen.Graphics2D.LineJoinMode lineJoin)
        {
            return (System.Drawing.Drawing2D.LineJoin)(int)lineJoin;
        }

        public static System.Drawing.Drawing2D.FillMode ToSysDrawFillMode(this PurplePen.Graphics2D.AreaFillMode fillMode)
        {
            return (System.Drawing.Drawing2D.FillMode)(int)fillMode;
        }

    }
}

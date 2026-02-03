using PdfSharp.Pdf.Advanced;
using PurplePen.Graphics2D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurplePen
{
    // Class to compute transformations for Move All.
    internal class MoveAllComputations
    {
        MoveAllControlsAction action;
        PointF[] points;
        float xOffset, yOffset;
        double scale;
        double rotation;

        // Type of move all action, plus the points that control it. This is 2 points for 
        // Move actions, and 4 points for Move + Scale and/or Rotate. The first of the second is a point that is 
        // after the first transform.
        public MoveAllComputations(MoveAllControlsAction action, PointF[] points)
        {
            this.action = action;

            if (action == MoveAllControlsAction.None) {
                xOffset = yOffset = 0;
                scale = 1.0;
                rotation = 0;
                return;
            }

            this.points = (PointF[]) points.Clone();
            xOffset = points[1].X - points[0].X;
            yOffset = points[1].Y - points[0].Y;
            if (action == MoveAllControlsAction.MoveScale || action == MoveAllControlsAction.MoveRotateScale) {
                // Determine amount of scale.
                double dist1 = Geometry.Distance(points[1], points[2]);
                double dist2 = Geometry.Distance(points[1], points[3]);
                if (dist1 == 0 || dist2 == 0) {
                    scale = 1.0;
                }
                else {
                    scale = Math.Abs(dist2 / dist1);
                }
            }
            else {
                scale = 1.0;
            }

            if (action == MoveAllControlsAction.MoveRotate || action == MoveAllControlsAction.MoveRotateScale) {
                // Determine amount of rotation.
                if ((points[1].X == points[2].X && points[1].Y == points[2].Y) || (points[1].X == points[3].X && points[1].Y == points[3].Y)) {
                    rotation = 0;
                }
                else {
                    double angle1 = Geometry.Angle(points[1], points[2]);
                    double angle2 = Geometry.Angle(points[1], points[3]);
                    rotation = Math.IEEERemainder(angle2 - angle1, 360.0);
                }
            }
            else {
                rotation = 0;
            }
        }

        public float XOffset => xOffset;
        public float YOffset => yOffset;
        public double Scale => scale;
        public double Rotation => rotation;

        public Matrix Matrix
        {
            get {
                Matrix matrix = new Matrix();

                if (action == MoveAllControlsAction.None)
                    return matrix;

                matrix.Translate(-points[0].X, - points[0].Y, MatrixOrder.Append);

                if (scale != 1.0) {
                    matrix.Scale((float)scale, (float)scale, MatrixOrder.Append);
                }
                if (rotation != 0) {
                    matrix.Rotate((float)rotation, MatrixOrder.Append);
                }

                matrix.Translate(points[1].X, points[1].Y, MatrixOrder.Append);

                return matrix;
            }
        }
    }
}

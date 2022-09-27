// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Copyright (c) 2007 Novell, Inc. (http://www.novell.com)
//
// Authors:
//  Chris Toshok (toshok@ximian.com)
//

using System;

namespace System.Drawing.Drawing2D {
    
    public class Matrix  {        
        double m11;
        double m12;
        double m21;
        double m22;
        double offsetX;
        double offsetY;
        
        public Matrix (double m11,
                       double m12,
                       double m21,
                       double m22,
                       double offsetX,
                       double offsetY)
        {
            this.m11 = m11;
            this.m12 = m12;
            this.m21 = m21;
            this.m22 = m22;
            this.offsetX = offsetX;
            this.offsetY = offsetY;
        }

        public Matrix()
        {
            this.m11 = 1.0;
            this.m22 = 1.0;
        }
        
        private Matrix Append (Matrix matrix)
        {
            double _m11;
            double _m21;
            double _m12;
            double _m22;
            double _offsetX;
            double _offsetY;
            
            _m11 = m11 * matrix.M11 + m12 * matrix.M21;
            _m12 = m11 * matrix.M12 + m12 * matrix.M22;
            _m21 = m21 * matrix.M11 + m22 * matrix.M21;
            _m22 = m21 * matrix.M12 + m22 * matrix.M22;
            
            _offsetX = offsetX * matrix.M11 + offsetY * matrix.M21 + matrix.OffsetX;
            _offsetY = offsetX * matrix.M12 + offsetY * matrix.M22 + matrix.OffsetY;

            return new Matrix(_m11, _m12, _m21, _m22, _offsetX, _offsetY);
        }

        private Matrix Prepend (Matrix matrix)
        {
            double _m11;
            double _m21;
            double _m12;
            double _m22;
            double _offsetX;
            double _offsetY;
            
            _m11 = matrix.M11 * m11 + matrix.M12 * m21;
            _m12 = matrix.M11 * m12 + matrix.M12 * m22;
            _m21 = matrix.M21 * m11 + matrix.M22 * m21;
            _m22 = matrix.M21 * m12 + matrix.M22 * m22;
            
            _offsetX = matrix.OffsetX * m11 + matrix.OffsetY * m21 + offsetX;
            _offsetY = matrix.OffsetX * m12 + matrix.OffsetY * m22 + offsetY;
            
            return new Matrix(_m11, _m12, _m21, _m22, _offsetX, _offsetY);
        }
        
        private void SetFrom(Matrix matrix)
        {
            m11 = matrix.m11;
            m12 = matrix.m12;
            m21 = matrix.m21;
            m22 = matrix.m22;
            offsetX = matrix.offsetX;
            offsetY = matrix.offsetY;
        }
        
        public bool Equals (Matrix value)
        {
            if (value == null)
                return false;

            return (m11 == value.M11 &&
                    m12 == value.M12 &&
                    m21 == value.M21 &&
                    m22 == value.M22 &&
                    offsetX == value.OffsetX &&
                    offsetY == value.OffsetY);
        }
        
        public override bool Equals (object o)
        {
            if (!(o is Matrix))
                return false;
            
            return Equals ((Matrix)o);
        }
        
        public static bool Equals (Matrix matrix1,
                                   Matrix matrix2)
        {
            return matrix1.Equals (matrix2);
        }
        
        public override int GetHashCode ()
        {
            throw new NotImplementedException ();
        }

        public Matrix Clone()
        {
            return new Matrix(m11, m12, m21, m22, offsetX, offsetY);
        }
        
        public void Invert ()
        {
            if (!HasInverse)
                throw new InvalidOperationException ("Transform is not invertible.");
            
            double d = Determinant;
            
            /* 1/(ad-bc)[d -b; -c a] */
            
            double _m11 = m22;
            double _m12 = -m12;
            double _m21 = -m21;
            double _m22 = m11;
            
            double _offsetX = m21 * offsetY - m22 * offsetX;
            double _offsetY = m12 * offsetX - m11 * offsetY;
            
            m11 = _m11 / d;
            m12 = _m12 / d;
            m21 = _m21 / d;
            m22 = _m22 / d;
            offsetX = _offsetX / d;
            offsetY = _offsetY / d;
        }
        
        public void Multiply(Matrix matrix, MatrixOrder order)
        {
            if (order == MatrixOrder.Append)
                SetFrom(this.Append(matrix));
            else
                SetFrom(this.Prepend(matrix));
        }

        public void Multiply(Matrix matrix)
        {
            Multiply(matrix, MatrixOrder.Prepend);
        }
        
        public void Rotate (float angle, MatrixOrder order)
        {
            // R_theta==[costheta -sintheta; sintheta costheta],    
            double theta = angle * Math.PI / 180;
            
            Matrix r_theta = new Matrix (Math.Cos (theta), Math.Sin(theta),
                                         -Math.Sin (theta), Math.Cos(theta),
                                         0, 0);
            
            Multiply(r_theta, order);
        }

        public void Rotate(float angle)
        {
            Rotate(angle, MatrixOrder.Prepend);
        }
        
        public void RotateAt (float angle, PointF point, MatrixOrder order)
        {
            Matrix m = Matrix.Identity;
            m.Translate (point.X, point.Y);
            m.Rotate (angle);
            m.Translate (-point.X, -point.Y);
            Multiply(m, order);
        }

        public void RotateAt(float angle, PointF point)
        {
            RotateAt(angle, point, MatrixOrder.Prepend);
        }
        
        public void Scale (double scaleX, double scaleY, MatrixOrder order)
        {
            Matrix scale = new Matrix (scaleX, 0,
                                       0, scaleY,
                                       0, 0);
            
            Multiply (scale, order);
        }

        public void Scale(double scaleX, double scaleY)
        {
            Scale(scaleX, scaleY, MatrixOrder.Prepend);
        }

        public void Shear (double skewX, double skewY, MatrixOrder order)
        {
            Matrix skew_m = new Matrix (1, Math.Tan (skewY * Math.PI / 180),
                                        Math.Tan (skewX * Math.PI / 180), 1,
                                        0, 0);
            Multiply (skew_m, order);
        }
        
        public void Shear (double skewX, double skewY)
        {
            Shear(skewX, skewY, MatrixOrder.Prepend);
        }

        public override string ToString ()
        {
            if (IsIdentity)
                return "Identity";
            else
                return string.Format ("{0},{1},{2},{3},{4},{5}",
                                      m11, m12, m21, m22, offsetX, offsetY);
        }
        
        public PointF Transform (PointF point)
        {
            float newX = (float) ((point.X * m11) + (point.Y * m21) + offsetX);
            float newY = (float) ((point.X * m12) + (point.Y * m22) + offsetY);
            return new PointF(newX, newY);
        }
        
        public void TransformPoints (PointF[] points)
        {
            for (int i = 0; i < points.Length; i ++)
                points[i] = Transform (points[i]);
        }

        public void Translate (double offsetX, double offsetY, MatrixOrder order)
        {
            Matrix m = new Matrix(1, 0, 0, 1, offsetX, offsetY);
            Multiply(m, order);
        }
        
        public void Translate (double offsetX, double offsetY)
        {
            Translate(offsetX, offsetY, MatrixOrder.Prepend);
        }
        
        public double Determinant {
            get { return m11 * m22 - m12 * m21; }
        }
        
        public bool HasInverse {
            get { return Determinant != 0; }
        }
        
        public static Matrix Identity {
            get { return new Matrix (1.0, 0.0, 0.0, 1.0, 0.0, 0.0); }
        }
        
        public bool IsIdentity {
            get { return Equals (Matrix.Identity); }
        }
        
        public double M11 { 
            get { return m11; }
            set { m11 = value; }
        }
        public double M12 { 
            get { return m12; }
            set { m12 = value; }
        }
        public double M21 { 
            get { return m21; }
            set { m21 = value; }
        }
        public double M22 { 
            get { return m22; }
            set { m22 = value; }
        }
        public double OffsetX { 
            get { return offsetX; }
            set { offsetX = value; }
        }
        public double OffsetY { 
            get { return offsetY; }
            set { offsetY = value; }
        }

        public float[] Elements {
            get {
                return new float[] {
                    (float) m11,
                    (float) m12, 
                    (float) m21,
                    (float) m22,
                    (float) offsetX,
                    (float) offsetY
                };
            }
        }
    }
    
}

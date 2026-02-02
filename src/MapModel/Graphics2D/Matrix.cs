// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
//	Chris Toshok (toshok@ximian.com)
//

namespace System.Drawing.Drawing2D
{
    public sealed class Matrix : IDisposable
    {
        double _m11;
        double _m12;
        double _m21;
        double _m22;
        double _offsetX;
        double _offsetY;

        public Matrix()
            :this(1.0, 0.0, 0.0, 1.0, 0.0, 0.0)
        {

        }

        public Matrix(float m11, float m12, float m21, float m22, float dx, float dy)
        {
            this._m11 = m11;
            this._m12 = m12;
            this._m21 = m21;
            this._m22 = m22;
            this._offsetX = dx;
            this._offsetY = dy;
        }

        private Matrix(double m11, double m12, double m21, double m22, double dx, double dy)
        {
            this._m11 = m11;
            this._m12 = m12;
            this._m21 = m21;
            this._m22 = m22;
            this._offsetX = dx;
            this._offsetY = dy;
        }

        public void Dispose()
        {
        }

        public Matrix Clone()
        {
            return new Matrix(_m11, _m12, _m21, _m22, _offsetX, _offsetY);
        }

        public float[] Elements {
            get {
                float[] elements = new float[6];
                elements[0] = (float)_m11;
                elements[1] = (float)_m12;
                elements[2] = (float)_m21;
                elements[3] = (float)_m22;
                elements[4] = (float)_offsetX;
                elements[5] = (float)_offsetY;
                return elements;
            }
        }

        public float OffsetX => (float) _offsetX;
        public float OffsetY => (float) _offsetY;

        public void Reset()
        {
            _m11 = 1.0;
            _m12 = 0.0;
            _m21 = 0.0;
            _m22 = 1.0;
            _offsetX = 0.0;
            _offsetY = 0.0;
        }

        private void Append(Matrix matrix)
        {
            double _m11;
            double _m21;
            double _m12;
            double _m22;
            double _offsetX;
            double _offsetY;

            _m11 = this._m11 * matrix._m11 + this._m12 * matrix._m21;
            _m12 = this._m11 * matrix._m12 + this._m12 * matrix._m22;
            _m21 = this._m21 * matrix._m11 + this._m22 * matrix._m21;
            _m22 = this._m21 * matrix._m12 + this._m22 * matrix._m22;

            _offsetX = this._offsetX * matrix._m11 + this._offsetY * matrix._m21 + matrix._offsetX;
            _offsetY = this._offsetX * matrix._m12 + this._offsetY * matrix._m22 + matrix._offsetY;

            this._m11 = _m11;
            this._m12 = _m12;
            this._m21 = _m21;
            this._m22 = _m22;
            this._offsetX = _offsetX;
            this._offsetY = _offsetY;
        }

        private void Prepend(Matrix matrix)
        {
            double _m11;
            double _m21;
            double _m12;
            double _m22;
            double _offsetX;
            double _offsetY;

            _m11 = matrix._m11 * this._m11 + matrix._m12 * this._m21;
            _m12 = matrix._m11 * this._m12 + matrix._m12 * this._m22;
            _m21 = matrix._m21 * this._m11 + matrix._m22 * this._m21;
            _m22 = matrix._m21 * this._m12 + matrix._m22 * this._m22;

            _offsetX = matrix._offsetX * this._m11 + matrix._offsetY * this._m21 + this._offsetX;
            _offsetY = matrix._offsetX * this._m12 + matrix._offsetY * this._m22 + this._offsetY;

            this._m11 = _m11;
            this._m12 = _m12;
            this._m21 = _m21;
            this._m22 = _m22;
            this._offsetX = _offsetX;
            this._offsetY = _offsetY;
        }

        public void Multiply(Matrix matrix) => Multiply(matrix, MatrixOrder.Prepend);

        public void Multiply(Matrix matrix, MatrixOrder order)
        {
            if (order == MatrixOrder.Prepend) {
                Prepend(matrix);
            }
            else {
                Append(matrix);
            }
        }

        public void Translate(float offsetX, float offsetY) => Translate(offsetX, offsetY, MatrixOrder.Prepend);

        public void Translate(float offsetX, float offsetY, MatrixOrder order)
        {
            Matrix m = new Matrix(1.0, 0.0, 0.0, 1.0, offsetX, offsetY);
            if (order == MatrixOrder.Prepend) {
                Prepend(m);
            }
            else {
                Append(m);
            }
        }

        public void Scale(float scaleX, float scaleY) => Scale(scaleX, scaleY, MatrixOrder.Prepend);

        public void Scale(float scaleX, float scaleY, MatrixOrder order)
        {
            Matrix m = new Matrix(scaleX, 0.0, 0.0, scaleY, 0.0, 0.0);
            if (order == MatrixOrder.Prepend) {
                Prepend(m);
            }
            else {
                Append(m);
            }
        }

        public void Shear(float shearX, float shearY) => Shear(shearX, shearY, MatrixOrder.Prepend);

        public void Shear(float shearX, float shearY, MatrixOrder order)
        {
            Matrix m = new Matrix(1.0, shearY, shearX, 1.0, 0.0, 0.0);
            if (order == MatrixOrder.Prepend) {
                Prepend(m);
            }
            else {
                Append(m);
            }
        }

        public void Rotate(float angle) => Rotate(angle, MatrixOrder.Prepend);

        public void Rotate(float angle, MatrixOrder order)
        {
            double theta = angle * Math.PI / 180;

            Matrix r_theta = new Matrix(Math.Cos(theta), Math.Sin(theta),
                             -Math.Sin(theta), Math.Cos(theta),
                             0, 0);

            if (order == MatrixOrder.Prepend) {
                Prepend(r_theta);
            }
            else {
                Append(r_theta);
            }
        }

        public void RotateAt(float angle, PointF point) => RotateAt(angle, point, MatrixOrder.Prepend);

        public void RotateAt(float angle, PointF point, MatrixOrder order)
        {
            if (order == MatrixOrder.Prepend) {
                Translate(point.X, point.Y, MatrixOrder.Prepend);
                Rotate(angle, MatrixOrder.Prepend);
                Translate(-point.X, -point.Y, MatrixOrder.Prepend);
            }
            else {
                Translate(-point.X, -point.Y, MatrixOrder.Append);
                Rotate(angle, MatrixOrder.Append);
                Translate(point.X, point.Y, MatrixOrder.Append);
            }
        }

        public void Invert()
        {
            if (!HasInverse)
                throw new InvalidOperationException("Transform is not invertible.");

            double d = Determinant;

            /* 1/(ad-bc)[d -b; -c a] */

            double _m11 = this._m22;
            double _m12 = -this._m12;
            double _m21 = -this._m21;
            double _m22 = this._m11;

            double _offsetX = this._m21 * this._offsetY - this._m22 * this._offsetX;
            double _offsetY = this._m12 * this._offsetX - this._m11 * this._offsetY;

            this._m11 = _m11 / d;
            this._m12 = _m12 / d;
            this._m21 = _m21 / d;
            this._m22 = _m22 / d;
            this._offsetX = _offsetX / d;
            this._offsetY = _offsetY / d;
        }

        private double Determinant {
            get { return _m11 * _m22 - _m12 * _m21; }
        }

        private bool HasInverse {
            get { return Determinant != 0; }
        }

        private PointF TransformPoint(PointF point)
        {
            return new PointF((float)(point.X * _m11 + point.Y * _m21 + _offsetX),
                              (float)(point.X * _m12 + point.Y * _m22 + _offsetY));
        }

        private PointF TransformVector(PointF point)
        {
            return new PointF((float)(point.X * _m11 + point.Y * _m21),
                              (float)(point.X * _m12 + point.Y * _m22));
        }

        public void TransformPoints(PointF[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));

            for (int i = 0; i < pts.Length; i++)
                pts[i] = TransformPoint(pts[i]);
        }

        public void TransformVectors(PointF[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));

            for (int i = 0; i < pts.Length; i++)
                pts[i] = TransformVector(pts[i]);
        }

        public bool IsInvertible {
            get {
                return HasInverse;
            }
        }

        public bool IsIdentity {
            get {
                return _m11 == 1.0 && _m12 == 0.0 && _m21 == 0.0 && _m22 == 1.0 && _offsetX == 0.0 && _offsetY == 0.0;
            }
        }

        public bool Equals(Matrix value)
        {
            return (_m11 == value._m11 &&
                _m12 == value._m12 &&
                _m21 == value._m21 &&
                _m22 == value._m22 &&
                _offsetX == value.OffsetX &&
                _offsetY == value.OffsetY);
        }

        public override bool Equals(object o)
        {
            if (!(o is Matrix))
                return false;

            return Equals((Matrix)o);
        }

        public override int GetHashCode()
        {
            unchecked {
                var hashCode = _m11.GetHashCode();
                hashCode = (hashCode * 397) ^ _m12.GetHashCode();
                hashCode = (hashCode * 397) ^ _m21.GetHashCode();
                hashCode = (hashCode * 397) ^ _m22.GetHashCode();
                hashCode = (hashCode * 397) ^ _offsetX.GetHashCode();
                hashCode = (hashCode * 397) ^ _offsetY.GetHashCode();
                return hashCode;
            }
        }
    }

    public enum MatrixOrder
    {
        Prepend = 0,
        Append = 1
    }
}

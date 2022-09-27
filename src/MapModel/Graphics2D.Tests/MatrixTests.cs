using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using NUnit.Framework;

namespace Graphics2D.Tests
{
    [TestFixture]
    public class MatrixTests
    {
        private PointF TransformPoint(Matrix matrix, PointF pt)
        {
            PointF[] a = new PointF[1] { pt };
            matrix.TransformPoints(a);
            return a[0];
        }

        private void CheckPoint(PointF expected, Matrix matrix, PointF pt)
        {
            PointF actual = TransformPoint(matrix, pt);
            Assert.AreEqual(expected.X, actual.X, 0.00001F);
            Assert.AreEqual(expected.Y, actual.Y, 0.00001F);
        }

        private void DumpElements(Matrix m)
        {
            Console.WriteLine("El0={0}, El1={1}, El2={2}, El3={3}, El4={4}, El5={5}", m.Elements[0], m.Elements[1], m.Elements[2], m.Elements[3], m.Elements[4], m.Elements[5]);
        }

        [Test]
        public void Translate()
        {
            Matrix matrix = new Matrix();
            matrix.Translate(7, -12);
            Assert.AreEqual(1, matrix.Elements[0]);
            Assert.AreEqual(0, matrix.Elements[1]);
            Assert.AreEqual(0, matrix.Elements[2]);
            Assert.AreEqual(1, matrix.Elements[3]);
            Assert.AreEqual(7, matrix.Elements[4]);
            Assert.AreEqual(-12, matrix.Elements[5]);

            Assert.AreEqual(new PointF(11, -2), TransformPoint(matrix, new PointF(4, 10)));
        }

        [Test]
        public void Scale()
        {
            Matrix matrix = new Matrix();
            matrix.Scale(2, -3);
            Assert.AreEqual(2, matrix.Elements[0]);
            Assert.AreEqual(0, matrix.Elements[1]);
            Assert.AreEqual(0, matrix.Elements[2]);
            Assert.AreEqual(-3, matrix.Elements[3]);
            Assert.AreEqual(0, matrix.Elements[4]);
            Assert.AreEqual(0, matrix.Elements[5]);

            Assert.AreEqual(new PointF(8, -30), TransformPoint(matrix, new PointF(4, 10)));
        }

        [Test]
        public void Rotate()
        {
            Matrix matrix = new Matrix();
            matrix.Rotate(30);
            Assert.AreEqual(0.8660254F, matrix.Elements[0], 0.0001F);
            Assert.AreEqual(0.5F, matrix.Elements[1], 0.0001F);
            Assert.AreEqual(-0.5F, matrix.Elements[2], 0.0001F);
            Assert.AreEqual(0.8660254F, matrix.Elements[3], 0.0001F);
            Assert.AreEqual(0, matrix.Elements[4]);
            Assert.AreEqual(0, matrix.Elements[5]);

           CheckPoint(new PointF(-1.535898F, 10.66025F), matrix, new PointF(4, 10));
        }

        [Test]
        public void RotateAt()
        {
            Matrix matrix = new Matrix();
            matrix.RotateAt(-30, new PointF(7, -3));
            Assert.AreEqual(0.8660254F, matrix.Elements[0], 0.0001F);
            Assert.AreEqual(-0.5F, matrix.Elements[1], 0.0001F);
            Assert.AreEqual(0.5F, matrix.Elements[2], 0.0001F);
            Assert.AreEqual(0.8660254F, matrix.Elements[3], 0.0001F);
            Assert.AreEqual(2.43782234, matrix.Elements[4], 0.0001F);
            Assert.AreEqual(3.09807658F, matrix.Elements[5], 0.0001F);

            CheckPoint(new PointF(10.901924133F, 9.75832939F), matrix, new PointF(4, 10));
        }

        [Test]
        public void Multiply()
        {
            Matrix m1 = new Matrix();
            m1.Translate(4, -8);
            Matrix m2 = new Matrix();
            m2.RotateAt(55, new PointF(4, 1));

            Matrix m3 = m1.Clone();
            m3.Multiply(m2, MatrixOrder.Append);
            Assert.AreEqual(0.5735764F, m3.Elements[0], 0.0001F);
            Assert.AreEqual(0.819152F, m3.Elements[1], 0.0001F);
            Assert.AreEqual(-0.819152F, m3.Elements[2], 0.0001F);
            Assert.AreEqual(0.5735764F, m3.Elements[3], 0.0001F);
            Assert.AreEqual(11.37237F,  m3.Elements[4], 0.0001F);
            Assert.AreEqual(-4.162188F, m3.Elements[5], 0.0001F);

            m3 = m1.Clone();
            m3.Multiply(m2, MatrixOrder.Prepend);
            Assert.AreEqual(0.5735764F, m3.Elements[0], 0.0001F);
            Assert.AreEqual(0.819152F, m3.Elements[1], 0.0001F);
            Assert.AreEqual(-0.819152F, m3.Elements[2], 0.0001F);
            Assert.AreEqual(0.5735764F, m3.Elements[3], 0.0001F);
            Assert.AreEqual(6.524846F, m3.Elements[4], 0.0001F);
            Assert.AreEqual(-10.85018F, m3.Elements[5], 0.0001F);

            m3 = m1.Clone();
            m3.Multiply(m2);
            Assert.AreEqual(0.5735764F, m3.Elements[0], 0.0001F);
            Assert.AreEqual(0.819152F, m3.Elements[1], 0.0001F);
            Assert.AreEqual(-0.819152F, m3.Elements[2], 0.0001F);
            Assert.AreEqual(0.5735764F, m3.Elements[3], 0.0001F);
            Assert.AreEqual(6.524846F, m3.Elements[4], 0.0001F);
            Assert.AreEqual(-10.85018F, m3.Elements[5], 0.0001F);
        }

        [Test]
        public void Compose()
        {
            Matrix m3 = new Matrix();
            m3.Translate(4, -8);
            m3.RotateAt(55, new PointF(4, 1), MatrixOrder.Append);
            Assert.AreEqual(0.5735764F, m3.Elements[0], 0.0001F);
            Assert.AreEqual(0.819152F, m3.Elements[1], 0.0001F);
            Assert.AreEqual(-0.819152F, m3.Elements[2], 0.0001F);
            Assert.AreEqual(0.5735764F, m3.Elements[3], 0.0001F);
            Assert.AreEqual(11.37237F, m3.Elements[4], 0.0001F);
            Assert.AreEqual(-4.162188F, m3.Elements[5], 0.0001F);

            m3 = new Matrix();
            m3.Translate(4, -8);
            m3.RotateAt(55, new PointF(4, 1), MatrixOrder.Prepend);
            Assert.AreEqual(0.5735764F, m3.Elements[0], 0.0001F);
            Assert.AreEqual(0.819152F, m3.Elements[1], 0.0001F);
            Assert.AreEqual(-0.819152F, m3.Elements[2], 0.0001F);
            Assert.AreEqual(0.5735764F, m3.Elements[3], 0.0001F);
            Assert.AreEqual(6.524846F, m3.Elements[4], 0.0001F);
            Assert.AreEqual(-10.85018F, m3.Elements[5], 0.0001F);

            m3 = new Matrix();
            m3.Translate(4, -8);
            m3.RotateAt(55, new PointF(4, 1), MatrixOrder.Prepend);
            Assert.AreEqual(0.5735764F, m3.Elements[0], 0.0001F);
            Assert.AreEqual(0.819152F, m3.Elements[1], 0.0001F);
            Assert.AreEqual(-0.819152F, m3.Elements[2], 0.0001F);
            Assert.AreEqual(0.5735764F, m3.Elements[3], 0.0001F);
            Assert.AreEqual(6.524846F, m3.Elements[4], 0.0001F);
            Assert.AreEqual(-10.85018F, m3.Elements[5], 0.0001F);
        }

    }
}

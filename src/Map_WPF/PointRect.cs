using System;

namespace PurplePen.MapModel
{
#if WPF
    // A point with "float".
    public struct PointF
    {
        private float x, y;

        public float X
        {
            get { return x; }
            set { x = value; }
        }

        public float Y
        {
            get { return y; }
            set { y = value; }
        }

        public PointF(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is PointF) {
                PointF other = (PointF) obj;
                return other == this;
            }
            else
                return false;
        }

        public static bool operator ==(PointF pt1, PointF pt2)
        {
            return pt1.x == pt2.x && pt1.y == pt2.y;
        }

        public static bool operator !=(PointF pt1, PointF pt2)
        {
            return !(pt1 == pt2);
        }

        public override string ToString()
        {
            return string.Format("{{X={0}, Y={1}}}", x, y);
        }
    }

    // A size with "float".
    public struct SizeF
    {
        private float width, height;

        public float Width
        {
            get { return width; }
        }

        public float Height
        {
            get { return height; }
        }

        public SizeF(float width, float height)
        {
            this.width = width;
            this.height = height;
        }

        public override int GetHashCode()
        {
            return width.GetHashCode() ^ height.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is SizeF) {
                SizeF other = (SizeF) obj;
                return other == this;
            }
            else
                return false;
        }

        public static bool operator ==(SizeF pt1, SizeF pt2)
        {
            return pt1.width == pt2.width && pt1.height == pt2.height;
        }

        public static bool operator !=(SizeF pt1, SizeF pt2)
        {
            return !(pt1 == pt2);
        }

        public override string ToString()
        {
            return string.Format("{{Width={0}, Height={1}}}", width, height);
        }

        public bool IsEmpty
        {
            get
            {
                return width == 0 && height == 0;
            }
        }
    }

    // A rectangle with float coordinates.
    public struct RectangleF
    {
        private float left, top, width, height;

        public static RectangleF Empty = new RectangleF();

        public RectangleF(float x, float y, float width, float height)
        {
            this.left = x;
            this.top = y;
            this.width = width;
            this.height = height;
        }

        public RectangleF(PointF location, SizeF size)
        {
            this.left = location.X;
            this.top = location.Y;
            this.width = size.Width;
            this.height = size.Height;
        }

        public static RectangleF FromLTRB(float left, float top, float right, float bottom)
        {
            return new RectangleF(left, top, right - left, bottom - top);
        }

        public static bool operator ==(RectangleF rect1, RectangleF rect2)
        {
            return rect1.left == rect2.left && rect1.top == rect2.top && rect1.width == rect2.width && rect1.height == rect2.height;
        }

        public static bool operator !=(RectangleF rect1, RectangleF rect2)
        {
            return !(rect1 == rect2);
        }

        public static implicit operator System.Windows.Rect(RectangleF source)
        {
            return new System.Windows.Rect(source.Left, source.Top, source.Width, source.Height);
        }

        public static explicit operator RectangleF(System.Windows.Rect source)
        {
            return new RectangleF((float) source.Left, (float) source.Top, (float) source.Width, (float) source.Height);
        }

        public override bool Equals(object obj)
        {
            if (obj is RectangleF) {
                RectangleF other = (RectangleF) obj;
                return this == other;
            }
            else
                return false;
        }

        public override int GetHashCode()
        {
            return left.GetHashCode() ^ top.GetHashCode() ^ Right.GetHashCode() ^ width.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{{X={0}, Y={1}, Width={2}, Height={3}}}", left, top, width, height);
        }

        public void Offset(float x, float y)
        {
            left += x;
            top += y;
        }

        public void Offset(PointF pos)
        {
            left += pos.X;
            top += pos.Y;
        }

        public void Inflate(float x, float y)
        {
            left -= x;
            width += x;
            top -= y;
            height += y;
        }

        public void Inflate(SizeF size)
        {
            Inflate(size.Width, size.Height);
        }

        public static RectangleF Inflate(RectangleF rect, float x, float y)
        {
            return new RectangleF(rect.left - x, rect.top - y, rect.width + x, rect.height + y);
        }

        public bool Contains(float x, float y)
        {
            return (x >= left && x <= Right && y >= top && y <= Bottom);
        }

        public bool Contains(PointF pt)
        {
            return Contains(pt.X, pt.Y);
        }

        public bool Contains(RectangleF rect)
        {
            float rectL = rect.Left, rectR = rect.Right, rectT = rect.Top, rectB = rect.Bottom;

            return (rectL >= left && rectL <= Right &&
                        rectR >= left && rectR >= Right &&
                        rectT >= top && rectT <= Bottom &&
                        rectB >= top && rectB <= Bottom);
        }

        public static RectangleF Union(RectangleF rect1, RectangleF rect2)
        {
            return RectangleF.FromLTRB(Math.Min(rect1.Left, rect2.Left),
                                                         Math.Min(rect1.Top, rect2.Top),
                                                         Math.Max(rect1.Right, rect2.Right),
                                                         Math.Max(rect1.Bottom, rect2.Bottom));
        }

        public static RectangleF Intersect(RectangleF rect1, RectangleF rect2)
        {
            float left = Math.Max(rect1.Left, rect2.Left);
            float top = Math.Max(rect1.Top, rect2.Top);
            float right = Math.Min(rect1.Right, rect2.Right);
            float bottom = Math.Min(rect1.Bottom, rect2.Bottom);

            if (left > right || top > bottom)
                return new RectangleF(0, 0, 0, 0);
            else
                return RectangleF.FromLTRB(left, top, right, bottom);
        }

        public void Intersect(RectangleF rect)
        {
            float right = Right;
            float bottom = Bottom;
            left = Math.Max(left, rect.Left);
            top = Math.Max(top, rect.Top);
            right = Math.Min(right, rect.Right);
            bottom = Math.Min(bottom, rect.Bottom);

            if (left >= right || top >= bottom)
                left = top = width = height = 0;
            else {
                width = right - left;
                height = bottom - top;
            }
        }

        public bool IntersectsWith(RectangleF rect)
        {
            float right = Right;
            float bottom = Bottom;
            float left = this.left;
            float top = this.top;
            left = Math.Max(left, rect.Left);
            top = Math.Max(top, rect.Top);
            right = Math.Min(right, rect.Right);
            bottom = Math.Min(bottom, rect.Bottom);

            return left < right && top < bottom;
        }

        public float Left
        {
            get { return left; }
        }

        public float X
        {
            get { return left; }
        }

        public float Top
        {
            get { return top; }
        }

        public float Y
        {
            get { return top; }
        }

        public float Width
        {
            get { return width; }
        }

        public float Height
        {
            get { return height; }
        }

        public float Right
        {
            get { return left + width; }
        }

        public float Bottom
        {
            get { return top + height; }
        }

        public PointF Location
        {
            get { return new PointF(left, top); }
        }

        public SizeF Size
        {
            get { return new SizeF(width, height); }
        }

        public bool IsEmpty
        {
            get { return width == 0 || height == 0; }
        }
    }

    // A color matrix
    public class ColorMatrix
    {
        private float[][] entries;

        public ColorMatrix()
        {
            entries = new float[5][];
            for (int i = 0; i < 5; ++i)
                entries[i] = new float[5];
        }

        public ColorMatrix(float[][] entries)
        {
            this.entries = entries;
        }

        public float this[int i, int j] {
            get {
                return entries[i][j];
            }
            set {
                entries[i][j] = value;
            }
        }
    }
#endif
}

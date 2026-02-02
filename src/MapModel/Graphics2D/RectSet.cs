using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;

namespace PurplePen.Graphics2D
{
    /// <summary>
    /// Maintains a set of rectangles that are disjoint. Allows adding and removing rectangles,
    /// maintaining the disjointness. When adding, no attempt is made to merge adjacent rectangles.
    /// </summary>
    public class RectSet
    {
        private List<RectangleF> rectangles;
        public int maxSize = 32; // Default maximum size.

        public RectSet()
        {
            rectangles = new List<RectangleF>();
        }

        public RectSet(RectangleF rect) : this()
        {
            Debug.Assert(rect.Left <= rect.Right && rect.Top <= rect.Bottom);
            rectangles.Add(rect);
        }

        public IEnumerable<RectangleF> Rectangles {
            get { return rectangles; }
        }

        public bool IsEmpty
        {
            get { return rectangles.Count == 0; }
        }

        // The maximum number of disjoint rectangles maintain. Beyond this limit,
        // the rectangles are combined into a single rectangle.
        public int MaxSize {
            get { return maxSize; }
            set { 
                if (value > 1)
                    maxSize = value;
            }
        }

        public void Add(RectangleF rect)
        {
            Debug.Assert(rect.Left <= rect.Right && rect.Top <= rect.Bottom);
            if (rect.IsEmpty)
                return;

            // If the rectangle is already contained in our existing rectangles, no need to do anything.
            foreach (RectangleF r in rectangles) {
                if (r.Contains(rect))
                    return;
            }

            // Remove all parts (could be whole rectangles) contained inside rect. This make sure that the rectangle
            // list is always disjoint.
            Subtract(rect);  

            rectangles.Add(rect);
            CheckMaxSize();
        }

        public void Add(RectSet rectSet)
        {
            foreach (RectangleF r in rectSet.Rectangles) {
                Add(r);
            }
        }

        public void Subtract(RectangleF rect)
        {
            Debug.Assert(rect.Left <= rect.Right && rect.Top <= rect.Bottom);

            List<RectangleF> newList = new List<RectangleF>();

            // Go through each existing rectangle, and subtract the new rectangle from it.
            foreach (RectangleF r in rectangles)
            {
                SubtractRectAndInsertNew(r, rect, newList);
            }

            rectangles = newList;
            CheckMaxSize();
        }

        public void Subtract(RectSet rectSet)
        {
            foreach (RectangleF r in rectSet.Rectangles) {
                Subtract(r);
            }
        }

        // Subtrace rectangle "subrect" from rectangle "orig", and add all remaining rectangles to "list".
        private void SubtractRectAndInsertNew(RectangleF orig, RectangleF subtract, List<RectangleF> list)
        {
            if (!orig.IntersectsWith(subtract)) {
                // Does not intersect; just keep the original rectangle.
                AddIfNotEmpty(list, orig);
                return;
            }

            if (subtract.Left <= orig.Left) {
                if (subtract.Right < orig.Right) {
                    AddIfNotEmpty(list, RectangleF.FromLTRB(subtract.Right, orig.Top, orig.Right, orig.Bottom));

                    if (subtract.Bottom < orig.Bottom) {
                        AddIfNotEmpty(list, RectangleF.FromLTRB(orig.Left, subtract.Bottom, subtract.Right, orig.Bottom));
                    }
                    if (subtract.Top > orig.Top) {
                        AddIfNotEmpty(list, RectangleF.FromLTRB(orig.Left, orig.Top, subtract.Right, subtract.Top));
                    }
                }
                else /* subtract.Right >= orig.Right */ {
                    if (subtract.Bottom < orig.Bottom) {
                        AddIfNotEmpty(list, RectangleF.FromLTRB(orig.Left, subtract.Bottom, orig.Right, orig.Bottom));
                    }
                    if (subtract.Top > orig.Top) {
                        AddIfNotEmpty(list, RectangleF.FromLTRB(orig.Left, orig.Top, orig.Right, subtract.Top));
                    }
                }
            } 
            else /* subtract.Left > orig.Left */ {
                AddIfNotEmpty(list, RectangleF.FromLTRB(orig.Left, orig.Top, subtract.Left, orig.Bottom));

                if (subtract.Right < orig.Right) {
                    AddIfNotEmpty(list, RectangleF.FromLTRB(subtract.Right, orig.Top, orig.Right, orig.Bottom));

                    if (subtract.Bottom < orig.Bottom) {
                        AddIfNotEmpty(list, RectangleF.FromLTRB(subtract.Left, subtract.Bottom, subtract.Right, orig.Bottom));
                    }
                    if (subtract.Top > orig.Top) {
                        AddIfNotEmpty(list, RectangleF.FromLTRB(subtract.Left, orig.Top, subtract.Right, subtract.Top));
                    }
                }
                else /* subtract.Right >= orig.Right */ {
                    if (subtract.Bottom < orig.Bottom) {
                        AddIfNotEmpty(list, RectangleF.FromLTRB(subtract.Left, subtract.Bottom, orig.Right, orig.Bottom));
                    }
                    if (subtract.Top > orig.Top) {
                        AddIfNotEmpty(list, RectangleF.FromLTRB(subtract.Left, orig.Top, orig.Right, subtract.Top));
                    }
                }
            }
        }

        private void AddIfNotEmpty(List<RectangleF> list, RectangleF rect)
        {
            Debug.Assert(rect.Left <= rect.Right && rect.Top <= rect.Bottom);
            if (rect.IsEmpty)
                return;

            list.Add(rect);
        }

        // If we have more than MaxSize, just store the union of all the rectangles. This
        // prevent bad worst case performance.
        private void CheckMaxSize()
        {
            if (rectangles.Count > maxSize) {
                // Reduce all the rectangles to their union.

                RectangleF union = rectangles[0];
                for (int i = 1; i < rectangles.Count; ++i) {
                    union = RectangleF.Union(union, rectangles[i]);
                }
                rectangles.Clear();
                rectangles.Add(union);
            }

        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("[{0} rects: ", rectangles.Count);
            for (int i = 0; i < rectangles.Count; ++i) {
                if (i != 0)
                    builder.Append(", ");
                builder.AppendFormat("({0},{1})-({2},{3})", rectangles[i].Left, rectangles[i].Top, rectangles[i].Right, rectangles[i].Bottom);
            }
            builder.Append("]");
            return builder.ToString();
        }
    }
}


using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;


namespace Graphics2D.Tests
{
    using PurplePen.Graphics2D;

    [TestFixture]
    public class RectSetTests
    {
        void AssertDisjoint(IEnumerable<RectangleF> rectangles) {
            RectangleF[] rects = rectangles.ToArray();

            for (int i = 0; i < rects.Length; ++i) {
                for (int j = i + 1; j < rects.Length; ++j) {
                    Assert.False(rects[i].IntersectsWith(rects[j]));
                }
            }
        }

        [Test]
        public void TestSubtract()
        {
            RectangleF orig = RectangleF.FromLTRB(1, 4, 5, 9);

            RectangleF sub = orig;

            RectSet set = new RectSet(orig);
            set.Subtract(sub);
            Assert.True(set.Rectangles.Count() == 0);

            sub = RectangleF.FromLTRB(0.5F, 2.5F, 6.3F, 10.5F);
            set = new RectSet(orig);
            set.Subtract(sub);
            Assert.True(set.Rectangles.Count() == 0);

            sub = RectangleF.FromLTRB(0.5F, 2.5F, 4.3F, 10.5F);
            set = new RectSet(orig);
            set.Subtract(sub);
            Assert.True(set.Rectangles.Count() == 1);
            Assert.AreEqual(RectangleF.FromLTRB(4.3F, 4, 5, 9), set.Rectangles.First());

            sub = RectangleF.FromLTRB(2.5F, 4.5F, 4.3F, 8F);
            set = new RectSet(orig);
            set.Subtract(sub);
            Assert.True(set.Rectangles.Count() == 4);
            AssertDisjoint(set.Rectangles);
            Assert.That(set.Rectangles, Is.EquivalentTo(new RectangleF[]
            {
                RectangleF.FromLTRB(1, 4, 2.5F, 9),
                RectangleF.FromLTRB(4.3F, 4, 5, 9),
                RectangleF.FromLTRB(2.5F, 4, 4.3F, 4.5F),
                RectangleF.FromLTRB(2.5F, 8, 4.3F, 9)
            }));


        }
    }
}


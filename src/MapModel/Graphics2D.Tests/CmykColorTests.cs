using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Graphics2D.Tests
{
    using PurplePen.Graphics2D;

    [TestFixture]
    public class CmykColorTests
    {
        private void RoundTripRgb(float red, float green, float blue)
        {
            CmykColor c = CmykColor.FromRgb(red, green, blue);
            CmykColor c2 = CmykColor.FromCmyk(c.Cyan, c.Magenta, c.Yellow, c.Black);
            Console.WriteLine("r={0}, g={1}, b={2}      c={3}, m={4}, y={5}, k={6}", c2.Red, c2.Green, c2.Blue, c2.Cyan, c2.Magenta, c2.Yellow, c2.Black);
            Assert.AreEqual(c2.Red, red, 0.001F);
            Assert.AreEqual(c2.Green, green, 0.001F);
            Assert.AreEqual(c2.Blue, blue, 0.001F);
        }

        [Test]
        public void RoundTripRgb()
        {
            RoundTripRgb(1, 1, 1);
            RoundTripRgb(0, 0, 0);
            RoundTripRgb(1, 0, 0);
            RoundTripRgb(1, 1, 0);
            RoundTripRgb(0, 1, 1);
            RoundTripRgb(0, 1, 0);
            RoundTripRgb(0.5F, 0.5F, 0.3F);
            RoundTripRgb(1, 0.5F, 1);
            RoundTripRgb(0, 0.2F, 0);
        }
    }
}

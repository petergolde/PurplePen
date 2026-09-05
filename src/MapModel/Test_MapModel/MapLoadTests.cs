#if TEST
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using NUnit.Framework;
using TestingUtils;

namespace PurplePen.MapModel.Tests
{
    [TestFixture]
    public class MapLoad
    {
        static Map ReadMap(string baseFileName)
        {
            string mapFileName = TestUtil.GetTestFile("loadmap\\" + baseFileName);
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("loadmap")));
            InputOutput.ReadFile(mapFileName, map);

            return map;
        }

        [Test]
        public void MissingColor()
        {
            Map map = ReadMap("missing_color.ocd");

            using (map.Read()) {
                LineSymDef tramway = (LineSymDef) map.SymdefFromSymbolId("515.2");
                Assert.IsNull(tramway.DoubleLines.doubleLeftColor);
                Assert.IsNull(tramway.DoubleLines.doubleRightColor);
            }
        }

    }

}

#endif //TEST

#if TEST
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using NUnit.Framework;
using TestingUtils;

namespace PurplePen.MapModel.Tests
{
    [TestFixture]
    public class RoundtripOcadTests 
    {
        [SetUp]
        public void Init()
        {
        }

        // Try to round trip an ocad file, and dump the original and the round
        // trip version. Returns true if the dumps compare equal.
        static bool RoundTripOcadFile(string mapOrigFileName)
        {
            string directory = Path.GetDirectoryName(mapOrigFileName);
            string basename = Path.GetFileNameWithoutExtension(mapOrigFileName);
            string mapNewFileName = directory + @"\" + basename + @"_new_temp.ocd";
            string dumpOrigFileName = directory + @"\" + basename + @"_dump_temp.txt";
            string dumpNewFileName = directory + @"\" + basename + @"_new_dump_temp.txt";
            MapFileFormat format;

            // Create and open the map file.
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(directory));
            format = InputOutput.ReadFile(mapOrigFileName, map);

            // Save the file again.
            InputOutput.WriteFile(mapNewFileName, map, format);

            // Dump the original file.
            using (TextWriter writer = new StreamWriter(dumpOrigFileName, false, System.Text.Encoding.UTF8)) {
                DebugCode.OcadDump dump = new DebugCode.OcadDump();
                dump.DumpFile(mapOrigFileName, writer);
            }

            // Dump the new file.
            using (TextWriter writer = new StreamWriter(dumpNewFileName, false, System.Text.Encoding.UTF8)) {
                DebugCode.OcadDump dump = new DebugCode.OcadDump();
                dump.DumpFile(mapNewFileName, writer);
            }

            return TestUtil.CompareTextFiles(dumpOrigFileName, dumpNewFileName);
        }

        void CheckTest(string filename)
        {
            string fullname = TestUtil.GetTestFile("io\\" + filename);
            bool ok = RoundTripOcadFile(fullname);
            Assert.IsTrue(ok, string.Format("Roundtrip test {0} did not compare correctly.", filename));
        }

        [Test]
        public void IconTest()
        {
            CheckTest("icontest.ocd");
        }

        [Test]
        public void Areas()
        {
            CheckTest("isomarea.ocd");
            CheckTest("holes.ocd");
        }

        [Test]
        public void Points()
        {
            CheckTest("isompoints.ocd");
        }


    }

}

#endif //TEST

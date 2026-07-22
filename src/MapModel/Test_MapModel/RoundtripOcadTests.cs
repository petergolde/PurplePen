/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

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

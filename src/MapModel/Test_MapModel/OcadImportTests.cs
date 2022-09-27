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
using NUnit.Framework;
using Color = System.Drawing.Color;

using TestingUtils;
using System.IO;

namespace PurplePen.MapModel.Tests
{
    [TestFixture]
    public class OcadImportTests
    {
        bool SameCoords(OcadCoord[] a, OcadCoord[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; ++i)
                if (a[i].x != b[i].x || a[i].y != b[i].y)
                    return false;

            return true;
        }
        [Test]
        public void FixOcadCoords() {
            OcadCoord[] coords, result, expected;
            int numHoles;
            bool anyCutouts;

            coords = new OcadCoord[] { new OcadCoord(0, 0), new OcadCoord(1, 0), new OcadCoord(2, 0), 
                                                           new OcadCoord(0, 0), new OcadCoord(1, 0), new OcadCoord(2, 0), 
                                                            new OcadCoord(0, 0), new OcadCoord(0, 0)};
            result = OcadImport.FixOcadCoords(coords, false, out numHoles, out anyCutouts);
            Assert.AreSame(coords, result);
            Assert.AreEqual(0, numHoles);

            coords = new OcadCoord[] { new OcadCoord(0, 0x100), new OcadCoord(1, 0x200), new OcadCoord(2, 0x300), 
                                                           new OcadCoord(2, 0x400), new OcadCoord(0, 0x500), new OcadCoord(1, 0x600), 
                                                            new OcadCoord(2, 0x700), new OcadCoord(0, 0x800)};
            expected = new OcadCoord[] { new OcadCoord(0, 0x100), new OcadCoord(0, 0x500), new OcadCoord(1, 0x600), 
                                                            new OcadCoord(2, 0x700), new OcadCoord(0, 0x800)};
            result = OcadImport.FixOcadCoords(coords, false, out numHoles, out anyCutouts);
            Assert.IsTrue(SameCoords(result, expected));
            Assert.AreEqual(0, numHoles);

            coords = new OcadCoord[] { new OcadCoord(0, 0x100), new OcadCoord(1, 0x200), new OcadCoord(2, 0x300), 
                                                           new OcadCoord(0, 0x400), new OcadCoord(0, 0x502), new OcadCoord(1, 0x600), 
                                                            new OcadCoord(2, 0x700), new OcadCoord(0, 0x800)};
            expected = new OcadCoord[] { new OcadCoord(0, 0x100), new OcadCoord(1, 0x200), new OcadCoord(2, 0x300), 
                                                           new OcadCoord(0, 0x400)};
            result = OcadImport.FixOcadCoords(coords, false, out numHoles, out anyCutouts);
            Assert.IsTrue(SameCoords(result, expected));
            Assert.AreEqual(0, numHoles);

            coords = new OcadCoord[] { new OcadCoord(0, 0x100), new OcadCoord(1, 0x200), new OcadCoord(2, 0x300), 
                                                           new OcadCoord(0, 0x400), new OcadCoord(0, 0x502), new OcadCoord(1, 0x600), 
                                                            new OcadCoord(2, 0x700), new OcadCoord(0, 0x800)};
            expected = new OcadCoord[] { new OcadCoord(0, 0x100), new OcadCoord(1, 0x200), new OcadCoord(2, 0x300), 
                                                           new OcadCoord(0, 0x400)};
            result = OcadImport.FixOcadCoords(coords, true, out numHoles, out anyCutouts);
            Assert.AreSame(coords, result);
            Assert.AreEqual(1, numHoles);

            coords = new OcadCoord[] { new OcadCoord(0, 0x100), new OcadCoord(1, 0x200), new OcadCoord(2, 0x300), 
                                                           new OcadCoord(0, 0x400), new OcadCoord(1, 0x502), new OcadCoord(0, 0x600), 
                                                            new OcadCoord(0, 0x700), new OcadCoord(0, 0x800)};
            expected = new OcadCoord[] { new OcadCoord(0, 0x100), new OcadCoord(1, 0x200), new OcadCoord(2, 0x300), 
                                                           new OcadCoord(0, 0x400), new OcadCoord(0, 0x602), 
                                                            new OcadCoord(0, 0x700), new OcadCoord(0, 0x800)};
            result = OcadImport.FixOcadCoords(coords, true, out numHoles, out anyCutouts);
            Assert.IsTrue(SameCoords(result, expected));
            Assert.AreEqual(1, numHoles);

            /*
            for (int i = 0; i < coords.Length; ++i)
                Console.WriteLine(coords[i]);
            Console.WriteLine();
            for (int i = 0; i < result.Length; ++i)
                Console.WriteLine(result[i]);
            Console.WriteLine();
            for (int i = 0; i < expected.Length; ++i)
                Console.WriteLine(expected[i]);
             * */
        }

        [Test]
        public void MissingFonts()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("ocadimport")));
            InputOutput.ReadFile(TestUtil.GetTestFile("ocadimport\\missingfont9.ocd"), map);

            string[] expected = { "Spyroclassic", "GeosansLight" };
            string[] missingFonts = map.MissingFonts;
            TestUtil.TestEnumerableAnyOrder(missingFonts, expected);

            map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("ocadimport")));
            InputOutput.ReadFile(TestUtil.GetTestFile("ocadimport\\missingfont6.ocd"), map);

            missingFonts = map.MissingFonts;
            TestUtil.TestEnumerableAnyOrder(missingFonts, expected);
        }

        [Test]
        public void UnrenderableObjects9()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("ocadimport")));
            InputOutput.ReadFile(TestUtil.GetTestFile("ocadimport\\nonrenderable9.ocd"), map);

            string[] expected = {
            };

            string[] nonrenderableObjects = map.NotRenderableObjects;

            CollectionAssert.AreEquivalent(nonrenderableObjects, expected);
        }

        [Test]
        public void UnrenderableObjects12()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("ocadimport")));
            InputOutput.ReadFile(TestUtil.GetTestFile("ocadimport\\nonrenderable12.ocd"), map);

            string[] expected = {
                "OCAD 12 feature: line decrease toward beginning of line (1.0:Decrease Dots, 3 objects)",
                "OCAD 12 feature: line decrease where symbol distances do not decrease (1.2:Decrease Dots 3, 2 objects)",
                "OCAD 12 feature: line decrease where symbol widths decrease (1.1:Decrease Dots 2, 1 object)",
                "OCAD 12 feature: opacity < 100% for layout layer (1 object)",
                "OCAD 12 feature: text in layout layer with bold font (1 object)",
                "OCAD 12 feature: text in layout layer with italic font (1 object)",
                "OCAD 12 feature: text in layout layer with alignment other than left (1 object)"
            };

            string[] nonrenderableObjects = map.NotRenderableObjects;

            CollectionAssert.AreEquivalent(expected, nonrenderableObjects);
        }

        [Test]
        public void UnrenderableObjects6()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("ocadimport")));
            InputOutput.ReadFile(TestUtil.GetTestFile("ocadimport\\nonrenderable6.ocd"), map);

            string[] expected = {
            };

            string[] nonrenderableObjects = map.NotRenderableObjects;

            TestUtil.TestEnumerableAnyOrder(nonrenderableObjects, expected);
        }

        [Test]
        public void FileInformation8()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("ocadimport")));
            InputOutput.ReadFile(TestUtil.GetTestFile("ocadimport\\fileinfo_8.ocd"), map);

            string expected = "Here is some file information.\r\n\r\n\r\nIt can have many lines,\r\nlike these.\r\n\r\nTHis line\thas tabs\r\n\tThis line has \t\t\tthree tabs.\r\nThis is the end.";

            string result;
            using (map.Read())
                result = map.FileInformation;

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FileInformation9()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("ocadimport")));
            InputOutput.ReadFile(TestUtil.GetTestFile("ocadimport\\fileinfo_9.ocd"), map);

            string expected = "This is my file information.\r\nIt has many lines.\r\n\r\nAnd some blank lines.\r\n\r\nAlso a line with tabs? Not this one.\r\nTHis line\thas tabs.\r\nAnd this does not.\r\n\r\n\r\n\tThis line has \t\t\tthree tabs.\r\nAt the end.\r\n";

            string result;
            using (map.Read())
                result = map.FileInformation;

            Assert.AreEqual(expected, result);

            string tempFile = TestUtil.GetTestFile("ocadimport\\fileinfo_9.ocd.tmp");

            InputOutput.WriteFile(tempFile, map, new MapFileFormat(MapFileFormatKind.OCAD, 9));
            try {
                Map newMap = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("ocadimport")));
                InputOutput.ReadFile(tempFile, newMap);

                using (newMap.Read())
                    result = newMap.FileInformation;

                Assert.AreEqual(expected, result);

                InputOutput.WriteFile(tempFile, map, new MapFileFormat(MapFileFormatKind.OCAD, 6));
                newMap = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("ocadimport")));
                InputOutput.ReadFile(tempFile, newMap);

                using (newMap.Read())
                    result = newMap.FileInformation;

                Assert.AreEqual(expected, result);
            }
            finally {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void EncryptDecryptMap()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("ocadimport")));
            InputOutput.ReadFile(TestUtil.GetTestFile("ocadimport\\fileinfo_9.ocd"), map);

            string expected = "This is my file information.\r\nIt has many lines.\r\n\r\nAnd some blank lines.\r\n\r\nAlso a line with tabs? Not this one.\r\nTHis line\thas tabs.\r\nAnd this does not.\r\n\r\n\r\n\tThis line has \t\t\tthree tabs.\r\nAt the end.\r\n";

            string result;
            using (map.Read())
                result = map.FileInformation;

            Assert.AreEqual(expected, result);

            string tempFile = TestUtil.GetTestFile("ocadimport\\fileinfo_9.ocd.tmp");

            InputOutput.WriteFileEncrypted(tempFile, map, new MapFileFormat(MapFileFormatKind.OCAD, 9));

            try {
                Map newMap = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("ocadimport")));
                InputOutput.ReadFile(tempFile, newMap);

                using (newMap.Read())
                    result = newMap.FileInformation;

                Assert.AreEqual(expected, result);

                InputOutput.WriteFileEncrypted(tempFile, map, new MapFileFormat(MapFileFormatKind.OCAD, 6));
                newMap = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("ocadimport")));
                InputOutput.ReadFile(tempFile, newMap);

                using (newMap.Read())
                    result = newMap.FileInformation;

                Assert.AreEqual(expected, result);
            }
            finally {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void ReadEncryptedMap()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("ocadimport")));
            InputOutput.ReadFile(TestUtil.GetTestFile("ocadimport\\encrypted_fileinfo_9.emylar"), map);

            string expected = "This is my file information.\r\nIt has many lines.\r\n\r\nAnd some blank lines.\r\n\r\nAlso a line with tabs? Not this one.\r\nTHis line\thas tabs.\r\nAnd this does not.\r\n\r\n\r\n\tThis line has \t\t\tthree tabs.\r\nAt the end.\r\n";

            string result;
            using (map.Read())
                result = map.FileInformation;

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void EncryptDecryptFiles()
        {
            string encryptedTempFile = TestUtil.GetTestFile("ocadimport\\fileinfo_8.ocd.encrypted");
            string decryptedTempFile = TestUtil.GetTestFile("ocadimport\\fileinfo_8.ocd.decrypted");

            try {
                InputOutput.EncryptFile(TestUtil.GetTestFile("ocadimport\\fileinfo_8.ocd"), encryptedTempFile);
                InputOutput.DecryptFile(encryptedTempFile, decryptedTempFile);

                Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("ocadimport")));
                InputOutput.ReadFile(encryptedTempFile, map);

                string expected = "Here is some file information.\r\n\r\n\r\nIt can have many lines,\r\nlike these.\r\n\r\nTHis line\thas tabs\r\n\tThis line has \t\t\tthree tabs.\r\nThis is the end.";

                string result;
                using (map.Read())
                    result = map.FileInformation;

                Assert.AreEqual(expected, result);

                using (map.Write())
                    map.Clear();

                InputOutput.ReadFile(decryptedTempFile, map);

                using (map.Read())
                    result = map.FileInformation;

                Assert.AreEqual(expected, result);
            }
            finally {
                File.Delete(encryptedTempFile);
                File.Delete(decryptedTempFile);
            }
        }

        [Test]
        public void NearestColor() {
            OcadExport exporter = new OcadExport();

            Color c = Color.FromArgb(45, 198, 255);
            Assert.AreEqual(exporter.NearestOcadColorSlow(c, true), exporter.NearestOcadColor(c, true));
            c = Color.FromArgb(0, 0, 0);
            Assert.AreEqual(exporter.NearestOcadColorSlow(c, true), exporter.NearestOcadColor(c, true));
            c = Color.FromArgb(33, 198, 255);
            Assert.AreEqual(exporter.NearestOcadColorSlow(c, true), exporter.NearestOcadColor(c, true));
            c = Color.FromArgb(255, 255, 255);
            Assert.AreEqual(exporter.NearestOcadColorSlow(c, true), exporter.NearestOcadColor(c, true));
            c = Color.FromArgb(186, 96, 0);
            Assert.AreEqual(exporter.NearestOcadColorSlow(c, true), exporter.NearestOcadColor(c, true));
        }

    }
}

#endif //TEST

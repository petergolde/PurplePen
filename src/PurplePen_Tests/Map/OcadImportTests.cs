/* Copyright (c) 2006-2007, Peter Golde
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
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestingUtils;

namespace PurplePen.MapModel.Tests
{
    [TestClass]
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
        [TestMethod]
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

        [TestMethod]
        public void MissingFonts()
        {
            Map map = new Map();
            InputOutput.ReadFile(TestUtil.GetTestFile("ocadimport\\missingfont9.ocd"), map);

            string[] expected = { "Spyroclassic", "GeosansLight" };
            string[] missingFonts = map.MissingFonts;
            TestUtil.TestEnumerableAnyOrder(missingFonts, expected);

            map = new Map();
            InputOutput.ReadFile(TestUtil.GetTestFile("ocadimport\\missingfont6.ocd"), map);

            missingFonts = map.MissingFonts;
            TestUtil.TestEnumerableAnyOrder(missingFonts, expected);
        }

        [TestMethod]
        public void UnrenderableObjects9()
        {
            Map map = new Map();
            InputOutput.ReadFile(TestUtil.GetTestFile("ocadimport\\nonrenderable9.ocd"), map);

            string[] expected = {
            };

            string[] nonrenderableObjects = map.NotRenderableObjects;

            CollectionAssert.AreEquivalent(nonrenderableObjects, expected);
        }

        [TestMethod]
        public void UnrenderableObjects6()
        {
            Map map = new Map();
            InputOutput.ReadFile(TestUtil.GetTestFile("ocadimport\\nonrenderable6.ocd"), map);

            string[] expected = {
            };

            string[] nonrenderableObjects = map.NotRenderableObjects;

            TestUtil.TestEnumerableAnyOrder(nonrenderableObjects, expected);
        }


    }
}

#endif //TEST

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
using System.Drawing;
using System.Windows.Forms;

using PurplePen.MapView;
using PurplePen.MapModel;

using TestingUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace PurplePen.Tests
{
    [TestClass]
    public class FindPurpleTests: TestFixtureBase
    {
        [TestMethod]
        public void IsPurple()
        {
            Assert.IsTrue(FindPurple.IsPurple(0, 1, 0, 0));
            Assert.IsTrue(FindPurple.IsPurple(0.43F, 0.78F, 0.22F, 0));
            Assert.IsFalse(FindPurple.IsPurple(0.95F, 0.30F, 0, 0));
            Assert.IsFalse(FindPurple.IsPurple(0, 1F, 0, 0.9F));
            Assert.IsFalse(FindPurple.IsPurple(0, 0F, 0, 0));
        }

        [TestMethod]
        public void FindPurpleByName()
        {
            Map map = new Map();
            using (map.Write()) {
                map.AddColor("Purple 50%", 14, 0, 0.5F, 0, 0);
                map.AddColor("Purple", 11, 0.2F, 1F, 0.1F, 0.08F);
                map.AddColor("Blue", 12, 0.95F, 0.35F, 0, 0);
                map.AddColor("Purplatci", 18, 0, 1F, 0, 0);
                map.AddColor("Black", 88, 0, 0, 0, 1F);
            }

            short ocadId;
            float c, m, y, k;
            List<SymColor> colorList;
            using (map.Read())
                colorList = new List<SymColor>(map.AllColors);

            Assert.IsTrue(FindPurple.FindPurpleColor(colorList, out ocadId, out c, out m, out y, out k));
            Assert.AreEqual(11, ocadId);
            Assert.AreEqual(0.2F, c);
            Assert.AreEqual(1F, m);
            Assert.AreEqual(0.1F, y);
            Assert.AreEqual(0.08F, k);
        }

        [TestMethod]
        public void FindPurpleByValue()
        {
            Map map = new Map();
            using (map.Write()) {
                map.AddColor("Purple 50%", 14, 0, 0.5F, 0, 0);
                map.AddColor("Purplicious", 11, 0.2F, 1F, 0.1F, 0.08F);
                map.AddColor("Blue", 12, 0.95F, 0.35F, 0, 0);
                map.AddColor("Black", 88, 0, 0, 0, 1F);
            }

            short ocadId;
            float c, m, y, k;
            List<SymColor> colorList;
            using (map.Read())
                colorList = new List<SymColor>(map.AllColors);

            Assert.IsTrue(FindPurple.FindPurpleColor(colorList, out ocadId, out c, out m, out y, out k));
            Assert.AreEqual(11, ocadId);
            Assert.AreEqual(0.2F, c);
            Assert.AreEqual(1F, m);
            Assert.AreEqual(0.1F, y);
            Assert.AreEqual(0.08F, k);
        }

        [TestMethod]
        public void NoPurple()
        {
            Map map = new Map();
            using (map.Write()) {
                map.AddColor("Yellow", 11, 0.0F, 0.25F, 0.79F, 0.08F);
                map.AddColor("Blue", 12, 0.95F, 0.35F, 0, 0);
                map.AddColor("Black", 88, 0, 0, 0, 1F);
            }

            short ocadId;
            float c, m, y, k;
            List<SymColor> colorList;
            using (map.Read())
                colorList = new List<SymColor>(map.AllColors);

            Assert.IsFalse(FindPurple.FindPurpleColor(colorList, out ocadId, out c, out m, out y, out k));
        }
    }
}

#endif //TEST

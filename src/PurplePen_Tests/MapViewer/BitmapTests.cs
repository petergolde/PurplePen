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
using System.Drawing.Imaging;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.MapView.Tests
{
    [TestClass]
    public class BitmapTests 
    {


        [TestMethod]
        public void CombineBitmaps()
        {
            string srcfile1 = TestUtil.GetTestFile("bitmap\\combine1.png");
            string srcfile2 = TestUtil.GetTestFile("bitmap\\combine2.png"); ;

            Bitmap src = (Bitmap)Image.FromFile(srcfile1);
            Bitmap dest = (Bitmap)Image.FromFile(srcfile2);
            Rectangle srcRect = new Rectangle(10, 10, 160, 180);
            Rectangle destRect = new Rectangle(50, 30, 160, 180);
            BitmapUtil.MergeBitmap(dest, destRect, src, srcRect);

            TestUtil.CheckBitmapsBase(dest, "bitmap\\combineout");
        }

        [TestMethod]
        public void LightenBitmap()
        {
            Bitmap src = (Bitmap) Image.FromFile(TestUtil.GetTestFile("bitmap\\lighten_src.png"));
            BitmapUtil.LightenBitmap(src, 0.5);
            TestUtil.CheckBitmapsBase(src, "bitmap\\lighten1");

            src = (Bitmap) Image.FromFile(TestUtil.GetTestFile("bitmap\\lighten_src.png"));
            BitmapUtil.LightenBitmap(src, 0.2);
            TestUtil.CheckBitmapsBase(src, "bitmap\\lighten2");

            src = (Bitmap) Image.FromFile(TestUtil.GetTestFile("bitmap\\lighten_src.png"));
            BitmapUtil.LightenBitmap(src, 0.8);
            TestUtil.CheckBitmapsBase(src, "bitmap\\lighten3");
        }
	
    }

}

#endif //TEST

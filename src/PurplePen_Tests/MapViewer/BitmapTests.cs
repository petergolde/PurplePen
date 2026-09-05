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
            string srcfile2 = TestUtil.GetTestFile("bitmap\\combine2.png"); 

            Bitmap src = (Bitmap)Image.FromFile(srcfile1);
            Bitmap dest = (Bitmap)Image.FromFile(srcfile2);
            Rectangle srcRect = new Rectangle(10, 10, 160, 180);
            Rectangle destRect = new Rectangle(50, 30, 160, 180);
            BitmapUtil.MergeBitmap(dest, destRect, src, srcRect);

            BitmapTestUtil.CheckBitmapsBase(dest, "bitmap\\combineout");
        }

        [TestMethod]
        public void LightenBitmap()
        {
            Bitmap src = (Bitmap) Image.FromFile(TestUtil.GetTestFile("bitmap\\lighten_src.png"));
            BitmapUtil.LightenBitmap(src, 0.5);
            BitmapTestUtil.CheckBitmapsBase(src, "bitmap\\lighten1");

            src = (Bitmap) Image.FromFile(TestUtil.GetTestFile("bitmap\\lighten_src.png"));
            BitmapUtil.LightenBitmap(src, 0.2);
            BitmapTestUtil.CheckBitmapsBase(src, "bitmap\\lighten2");

            src = (Bitmap) Image.FromFile(TestUtil.GetTestFile("bitmap\\lighten_src.png"));
            BitmapUtil.LightenBitmap(src, 0.8);
            BitmapTestUtil.CheckBitmapsBase(src, "bitmap\\lighten3");
        }
	
    }

}

#endif //TEST

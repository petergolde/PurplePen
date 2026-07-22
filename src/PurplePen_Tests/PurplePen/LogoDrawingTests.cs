using Microsoft.VisualStudio.TestTools.UnitTesting;
using PurplePen;
using PurplePen.Graphics2D;
using PurplePen.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestingUtils;

namespace PurplePen.Tests
{
    [TestClass]

    public class LogoDrawingTests: TestFixtureBase
    {
        [TestMethod]
        public void DrawLogo1()
        {
            string baseline = TestUtil.GetTestFile("logo\\logo1_baseline.png");
            string testfile = TestUtil.GetTestFile("logo\\logo1_temp.png");

            IBitmapGraphicsTarget bmTarget = Services.BitmapGraphicsTargetProvider.CreateBitmapGraphicsTarget(559, 182, CmykColor.FromCmyk(0, 0, 0, 0), DefaultColorConverter.Instance);
            LogoDrawing.DrawPurplePenLogo(bmTarget, new System.Drawing.RectangleF(0, 0, 559, 182));
            IGraphicsBitmap bitmap = bmTarget.FinishBitmap();
            using (Stream stream = new FileStream(testfile, FileMode.Create, FileAccess.Write)) {
                bitmap.WriteToStream(GraphicsBitmapFormat.PNG, stream, 100);
            }

            System.Drawing.Bitmap testBitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(testfile);
            BitmapTestUtil.CompareBitmapBaseline(testBitmap, baseline);
            testBitmap.Dispose();
            File.Delete(testfile);
        }
    }
}

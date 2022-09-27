
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

using Foundation;
using UIKit;
using CoreGraphics;

using NUnit.Framework;

namespace MapiOS.Tests
{
    [TestFixture]
    public class RenderingTests 
    {
        // Verifies a test file. Returns true on success, false on failure. In the failure case, 
        // a difference bitmap is written out.
        static bool VerifyTestFile(string testName, bool usePatternBitmaps, bool antiAlias, bool testLightenedColor, bool roundtripToOcadFile, int minOcadVersion, int maxOcadVersion)
        {
            string pngFileName;
            string mapFileName;
            string directoryName;
            RectangleF mapArea;
            Size size;
            
            // Read the test file, and get the other file names and the area.
            string testFileFullPath = Path.Combine(TestUtil.TestInputFilesDirectory, testName);
            directoryName = Path.GetDirectoryName(testFileFullPath);
            using (StreamReader reader = new StreamReader(testFileFullPath)) {
                mapFileName = reader.ReadLine();
                pngFileName = reader.ReadLine();
                float left, right, top, bottom;
                string area = reader.ReadLine();
                string[] coords = area.Split(',');
                left = float.Parse(coords[0]); bottom = float.Parse(coords[1]); right = float.Parse(coords[2]); top = float.Parse(coords[3]);
                mapArea = new RectangleF(left, top, right - left, bottom - top);
                string sizeLine = reader.ReadLine();
                coords = sizeLine.Split(',');
                size = new Size(int.Parse(coords[0]), int.Parse(coords[1]));
            }


            // Create and open the map file.
            Map map = new Map(new IOS_TextMetrics(), new IOS_FileLoader(directoryName));
            InputOutput.ReadFile(Path.Combine(directoryName, mapFileName), map);
            
            // Draw into a new bitmap.
            UIImage bitmapNew = TestUtil.RenderMapToImage(map, size, mapArea, usePatternBitmaps, antiAlias);
            string baseName = TestUtil.GetBaseName(Path.Combine(directoryName, pngFileName));

            TestUtil.CheckAgainstBaseline(baseName, bitmapNew);
            bitmapNew.Dispose();
            bitmapNew = null;

#if false
            if (testLightenedColor) {
                using (map.Write()) {
                    PurplePen.MapModel.ColorMatrix colorMatrix = new PurplePen.MapModel.ColorMatrix(new float[][] {
                        new float[] {0.4F,  0,  0,  0, 0},
                        new float[] {0,  0.4F,  0,  0, 0},
                        new float[] {0,  0,  0.4F,  0, 0},
                        new float[] {0,  0,  0,  1, 0},
                        new float[] {0.6F, 0.6F, 0.6F, 0, 1}
                    });
                    map.ColorMatrix = colorMatrix;
                }
                
                string lightenedPngFileName = Path.Combine(Path.GetDirectoryName(pngFileName), Path.GetFileNameWithoutExtension(pngFileName) + "_light.png");
                Bitmap bitmapLight = RenderBitmap(tempPdfFileName, map, size, mapArea, usePatternBitmaps);
                TestUtil.CompareBitmapBaseline(bitmapLight, lightenedPngFileName);
                bitmapLight.Dispose();
                bitmapLight = null;
            }
            
            if (roundtripToOcadFile) {
                for (int version = minOcadVersion; version <= maxOcadVersion; ++version) {  
                    // Save and load to a temp file name.
                    InputOutput.WriteFile(ocadFileName, map, version);
                    
                    // Create and open the map file.
                    map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("pdfrender")));
                    InputOutput.ReadFile(ocadFileName, map);
                    
                    // Draw into a new bitmap.
                    bitmapNew = RenderBitmap(tempPdfFileName, map, size, mapArea, usePatternBitmaps);
                    
                    TestUtil.CompareBitmapBaseline(bitmapNew, pngFileName);
                    
                    File.Delete(ocadFileName);
                }
            }

#endif
            return true;
        }
        
        
        void CheckTest(string testFileName, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion)
        {
            VerifyTestFile("iosrendering/" + testFileName, true, true, testLightenedColor, roundtripToOcad, minOcadVersion, maxOcadVersion);
        }
        
        void CheckTestNoPatternBitmaps(string testFileName, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion) {
            VerifyTestFile("iosrendering/" + testFileName, false, true, testLightenedColor, roundtripToOcad, minOcadVersion, maxOcadVersion);
        }

        [Test]
        public void TestWest() {
            CheckTest("teanwest.txt", false, false, 9, 10);
        }

        [Test]
        public void LineSymbols()
        {
            CheckTest("isomlines.txt", true, true, 6, 11);
            CheckTest("isomlines9.txt", true, true, 6, 11);
        }
        
        [Test]
        public void Fences()
        {
            CheckTest("fences.txt", false, true, 6, 11);
            CheckTest("fences9.txt", false, true, 6, 11);
        }
        
        [Test]
        public void FramingLines()
        {
            CheckTest("framingline-test.txt", false, true, 6, 11);
            CheckTest("framingline-test9.txt", false, true, 6, 11);
        }
        
        [Test]
        public void DashLines()
        {
            CheckTest("dashline.txt", false, true, 6, 11);
            CheckTest("dashline9.txt", false, true, 6, 11);
        }
        
        [Test]
        public void PointSymbols()
        {
            CheckTest("isompoints.txt", true, true, 6, 11);
            //CheckTest("isompoints_9.txt", false, true, 6, 11);
        }
        
        [Test]
        public void AreaSymbols()
        {
            CheckTest("isomarea.txt", true, true, 6, 11);
            CheckTest("isomarea9.txt", true, true, 6, 11);
        }
        
        [Test]
        public void AreaHoles()
        {
            CheckTest("holes.txt", false, true, 6, 11);
            CheckTest("holes9.txt", false, true, 6, 11);
        }
        
        [Test]
        public void CutCircles()
        {
            CheckTest("cutcircles.txt", false, true, 6, 11);
            //CheckTest("cutcircles9.txt", false, false, 6);    //OCAD 9 has some strange problems with cut circles...
        }
        
        [Test]
        public void HiddenSymbols()
        {
            CheckTest("hiddensymbols.txt", false, true, 6, 11);
            CheckTest("hiddensymbols9.txt", false, true, 6, 11);
        }
        
        
        [Test]
        public void RotatedAreas()
        {
            CheckTest("rotarea-test.txt", false, true, 6, 11);
            CheckTest("rotarea-test9.txt", false, true, 6, 11);
        }
        

        [Test]
        public void OffsetAreaPattern() {
            CheckTest("offsetpattern.txt", false, true, 6, 11);
        }

        [Test]
        public void OffsetAreaPatternNoBitmap() {
            CheckTestNoPatternBitmaps("offsetpattern_nopatbm.txt", false, true, 6, 11);
        }


        [Test]
        public void OffsetAreaPatternRotated() {
            CheckTest("offsetpatternrot.txt", false, true, 6, 11);
        }
        
        [Test]
        public void OffsetAreaPatternRotated2() {
            CheckTest("offsetpatternrot2.txt", false, true, 6, 11);
        }
        
        [Test]
        public void OffsetAreaPatternRotatedNoBitmap() {
            CheckTestNoPatternBitmaps("offsetpatternrot_nopatbm.txt", false, true, 6, 11);
        }
        
        [Test]
        public void OffsetAreaPatternRotated2NoBitmap() {
            CheckTestNoPatternBitmaps("offsetpatternrot2_nopatbm.txt", false, true, 6, 11);
        }
  


    }

}

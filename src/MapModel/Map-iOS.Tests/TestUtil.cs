using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

using Foundation;
using UIKit;
using CoreGraphics;

using NUnit.Framework;

namespace MapiOS.Tests
{
    public static class TestUtil
    {
        public static string TestInputFilesDirectory;
        public static string TestOutputFilesDirectory;
        public static string TestOutputImageDirectory;

        static TestUtil() 
        {
            TestInputFilesDirectory = Path.Combine (Environment.GetFolderPath(Environment.SpecialFolder.Personal), "TestFiles");
            TestOutputFilesDirectory = Path.Combine (Environment.GetFolderPath(Environment.SpecialFolder.Personal), "TestOutput");
            TestOutputImageDirectory = Path.Combine(TestOutputFilesDirectory, "Images");
            Directory.CreateDirectory(TestOutputFilesDirectory);
            Directory.CreateDirectory(TestOutputImageDirectory);
        }

        public static UIImage CreateImage(Action<IGraphicsTarget> drawProc, int width, int height)
        {
            UIImage image;
            
            UIGraphics.BeginImageContextWithOptions(new SizeF(width, height), true, 1.0F);
            using (CGContext context = UIGraphics.GetCurrentContext())
            {
                context.SetFillColor(1, 1, 1, 1);
                context.FillRect(new RectangleF(0, 0, width, height));
                
                IGraphicsTarget grTarget = new IOS_GraphicsTarget(context);
                drawProc(grTarget);
                image = UIGraphics.GetImageFromCurrentImageContext();
            }
            
            return image;
        }

        public static UIImage CreateImage(int width, RectangleF drawingRectangle, Action<IGraphicsTarget> drawProc)
        {
            int height = (int)Math.Ceiling(width * drawingRectangle.Height / drawingRectangle.Width);
            
            // Calculate the transform matrix.
            PointF midpoint = new PointF(width / 2.0F, height / 2.0F);
            float scaleFactor = (float)width / drawingRectangle.Width;
            PointF centerPoint = new PointF((drawingRectangle.Left + drawingRectangle.Right) / 2, (drawingRectangle.Top + drawingRectangle.Bottom) / 2);
            Matrix matrix = new Matrix();
            matrix.Translate(midpoint.X, midpoint.Y, MatrixOrder.Prepend);
            matrix.Scale(scaleFactor, -scaleFactor, MatrixOrder.Prepend);  // y scale is negative to get to cartesian orientation.
            matrix.Translate(-centerPoint.X, -centerPoint.Y, MatrixOrder.Prepend);

            UIImage image;
            
            UIGraphics.BeginImageContextWithOptions(new SizeF(width, height), true, 1.0F);
            using (CGContext context = UIGraphics.GetCurrentContext())
            {
                context.SetFillColor(1, 1, 1, 1);
                context.FillRect(new RectangleF(0, 0, width, height));
                
                IGraphicsTarget grTarget = new IOS_GraphicsTarget(context);
                grTarget.PushTransform(matrix);
                drawProc(grTarget);
                grTarget.PopTransform();
                image = UIGraphics.GetImageFromCurrentImageContext();
            }
            
            return image;
        }

        public static IGraphicsBitmap LoadInputBitmap(string name)
        {
            var fileLoader = new IOS_FileLoader(TestInputFilesDirectory);
            return fileLoader.LoadBitmap(name, false);
        }
        
        public static void EraseAllOutputImages()
        {
            foreach (string fileName in Directory.EnumerateFiles(TestOutputImageDirectory, "*", SearchOption.AllDirectories)) {
                File.Delete(fileName);
            }
        }

        public static string[] GetAllOutputImageBaseNames()
        {
            return (from fn in Directory.EnumerateFiles(TestOutputImageDirectory, "*", SearchOption.AllDirectories)
                    select GetBaseName(fn)).ToArray();
        }

        public static string GetBaseName(string fn)
        {
            if (fn.StartsWith(TestOutputImageDirectory))
                fn = fn.Substring(TestOutputImageDirectory.Length + 1);
            else if (fn.StartsWith(TestInputFilesDirectory))
                fn = fn.Substring(TestInputFilesDirectory.Length + 1);

            if (Path.HasExtension(fn)) {
                int dotLocation = fn.LastIndexOf('.');
                fn = fn.Substring(0, dotLocation);
            }

            return fn;
        }

        public static string GetOutputFileName(string baseName)
        {
            return Path.Combine(TestOutputImageDirectory, baseName + ".png");
        }

        public static string GetBaselineFileName(string baseName)
        {
            return Path.Combine(TestInputFilesDirectory, baseName + "_baseline.png");
        }

        public static UIImage LoadImageFromBaseName(string baseName)
        {
            return UIImage.FromFile(GetOutputFileName(baseName));
        }

        public static UIImage LoadBaselineImageFromBaseName(string baseName)
        {
            return UIImage.FromFile(GetBaselineFileName(baseName));
        }

        public static void CopyNewFileToBaseline(string baseName)
        {
            File.Copy(GetOutputFileName(baseName), GetBaselineFileName(baseName), true);
        }

        public static UIViewController ShowImageAndBaseline(string baseName)
        {
            return new TestImageViewController(baseName);
        }
        
        public static void SaveOutputImage(UIImage image, string baseName)
        {
            NSData data = image.AsPNG();
            NSError error;

            string outputFileName = GetOutputFileName(baseName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFileName));

            bool result = data.Save(GetOutputFileName(baseName), false, out error);
            Assert.IsTrue(result);
        }

        public static UIImage RenderMapToImage(Map map, Size bitmapSize, RectangleF mapArea, bool usePatternBitmaps, bool antiAlias)
        {
            // Calculate the transform matrix.
            PointF midpoint = new PointF(bitmapSize.Width / 2.0F, bitmapSize.Height / 2.0F);
            float scaleFactor = (float) bitmapSize.Width / mapArea.Width;
            PointF centerPoint = new PointF((mapArea.Left + mapArea.Right) / 2, (mapArea.Top + mapArea.Bottom) / 2);
            Matrix matrix = new Matrix();
            matrix.Translate(midpoint.X, midpoint.Y, MatrixOrder.Prepend);
            matrix.Scale(scaleFactor, -scaleFactor, MatrixOrder.Prepend);  // y scale is negative to get to cartesian orientation.
            matrix.Translate(-centerPoint.X, -centerPoint.Y, MatrixOrder.Prepend);
            
            RenderOptions renderOpts = new RenderOptions();
            renderOpts.usePatternBitmaps = usePatternBitmaps;
            renderOpts.minResolution = mapArea.Width / (float)bitmapSize.Width;            // Draw into a new bitmap.
            
            return TestUtil.CreateImage(grTarget =>
                                        {
                grTarget.PushTransform(matrix);
                grTarget.PushAntiAliasing(antiAlias);
                using (map.Read())
                    map.Draw(grTarget, mapArea, renderOpts, null);
                grTarget.PopAntiAliasing();
                grTarget.PopTransform();
            }, bitmapSize.Width, bitmapSize.Height);
        }


        private static CGColorSpace colorSpaceRGB = CGColorSpace.CreateDeviceRGB();

        public static byte[] GetImageBytes(UIImage image)
        {
            CGImage cgImage = image.CGImage;
            byte[] bytes = new byte[cgImage.Width * cgImage.Height * 4];
            CGBitmapContext bitmapContext = new CGBitmapContext(bytes, cgImage.Width, cgImage.Height, 8, cgImage.Width * 4, colorSpaceRGB, CGImageAlphaInfo.PremultipliedLast);
            bitmapContext.DrawImage(new RectangleF(0, 0, cgImage.Width, cgImage.Height), cgImage);
            bitmapContext.Dispose();
            return bytes;
        }

        public static bool AreBitmapsEqual(UIImage image1, UIImage image2)
        {
            byte[] bytes1 = GetImageBytes(image1);
            byte[] bytes2 = GetImageBytes(image2);

            if (bytes1.Length != bytes2.Length)
                return false;
            for (int i = 0; i < bytes1.Length; ++i) {
                if (bytes1[i] != bytes2[i])
                    return false;
            }

            return true;
        }

        public static UIImage BitmapDifference(UIImage image1, UIImage image2)
        {
            byte[] bytes1 = GetImageBytes(image1);
            byte[] bytes2 = GetImageBytes(image2);
            int length = Math.Min(bytes1.Length, bytes2.Length);
            byte[] diffBytes = new byte[length];

            for (int i = 0; i < length; ++i) {
                if (bytes1[i] != bytes2[i]) {
                    int offset = i & ~3;
                    diffBytes[offset] = 255;
                    diffBytes[offset + 3] = 255;  // Set pixel to RED!
                }
            }

            CGBitmapContext bitmapContext;
            CGImage protoImage = (bytes1.Length < bytes2.Length) ? image1.CGImage : image2.CGImage;
            CGImage diffImage;
            bitmapContext = new CGBitmapContext(diffBytes, protoImage.Width, protoImage.Height, 8, protoImage.Width * 4, colorSpaceRGB, CGImageAlphaInfo.PremultipliedLast);
            diffImage = bitmapContext.ToImage();
            bitmapContext.Dispose();

            return new UIImage(diffImage);
        }

        public static void CheckAgainstBaseline(string baseName, UIImage newImage)
        {
            string baselineFile = Path.Combine(TestInputFilesDirectory, baseName + "_baseline.png");
            if (!File.Exists(baselineFile)) {
                // There is no baseline file. Save the new image into the output image directory and assert.
                SaveOutputImage(newImage, baseName);
                Assert.Fail(string.Format("No baseline exists for image '{0}'", baseName));
                return;
            }

            UIImage baselineImage = UIImage.FromFile(baselineFile);
            if (! AreBitmapsEqual(newImage, baselineImage)) {
                // The new image doesn't match the baseline. Save the new image into the
                // output image directory, then Assert.
                SaveOutputImage(newImage, baseName);
                Assert.Fail(string.Format("Image '{0}' does not match baseline", baseName));
                return;
            }
        }
    }

}


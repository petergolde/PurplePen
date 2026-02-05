using NUnit.Framework;
using System;
using System.IO;
using TestingUtils;

namespace PdfConverter.Tests
{
    [TestFixture, NonParallelizable]
    public class ConvertPdfs
    {
#if NETFRAMEWORK
        string platform = "net48";
#else
        string platform = "net10";
#endif

        string GetRandomHex8()
        {
            var rnd = new System.Random();
            var bytes = new byte[4];
            rnd.NextBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        public void CheckPdfConversion(int dpi, string pdfFile, string baselineImage)
        {
            string testDirectory = Path.Combine(TestUtil.GetTestFileDirectory(), "pdfconverter");
            string fullPdfPath = TestUtil.GetTestFile(pdfFile);

            // Convert the PDF to "output_tempnew.png".
            string tempOutputFile = Path.Combine(TestUtil.GetTestFileDirectory(), $"pdfconverter\\output_{GetRandomHex8()}_tempnew.png");
            if (File.Exists(tempOutputFile))
                File.Delete(tempOutputFile);

            int returnValue = PurplePen.PdfConverter.Program.Main(new string[] {
                dpi.ToString(),
                fullPdfPath,
                Path.Combine(testDirectory, tempOutputFile)
            });

            try {
                Assert.AreEqual(0, returnValue);
                Assert.IsTrue(File.Exists(tempOutputFile));

                TestUtil.CompareBitmapBaseline(tempOutputFile, TestUtil.GetTestFile(baselineImage));
            }
            finally {
                if (File.Exists(tempOutputFile))
                    File.Delete(tempOutputFile);
            }
        }

        [Test]
        public void UWLetter()
        {
            CheckPdfConversion(600, "pdfconverter\\UWLetter.pdf", $"pdfconverter\\UWLetter_{platform}_baseline.png");
        }


        [Test]
        public void Teanaway()
        {
            CheckPdfConversion(400, "pdfconverter\\TeanawayA2.pdf", $"pdfconverter\\TeanawayA2_{platform}_baseline.png");
        }


        [Test]
        public void Tengesdalsia()
        {
            CheckPdfConversion(300, "pdfconverter\\tengesdalslia.pdf", $"pdfconverter\\tengesdalsia_{platform}_baseline.png");
        }
    }
}

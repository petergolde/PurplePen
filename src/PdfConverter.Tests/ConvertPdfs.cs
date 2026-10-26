using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using PurplePen.PdfConverter;
using TestingUtils;

namespace PdfConverter.Tests
{
    [TestFixture, NonParallelizable]
    public class ConvertPdfs
    {
        string platform = "net10";

        string GetRandomHex8()
        {
            var rnd = new System.Random();
            var bytes = new byte[4];
            rnd.NextBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        // Get the name of a temporary output file in the pdfconverter test directory.
        string GetTempOutputFile()
        {
            return Path.Combine(TestUtil.GetTestFileDirectory(), $"pdfconverter\\output_{GetRandomHex8()}_tempnew.png");
        }

        public void CheckPdfConversion(int dpi, string pdfFile, string baselineImage)
        {
            string fullPdfPath = TestUtil.GetTestFile(pdfFile);

            // Convert the PDF to "output_tempnew.png".
            string tempOutputFile = GetTempOutputFile();
            if (File.Exists(tempOutputFile))
                File.Delete(tempOutputFile);

            try {
                PdfConversion.Convert(dpi, fullPdfPath, tempOutputFile);
                Assert.IsTrue(File.Exists(tempOutputFile));

                BitmapTestUtil.CompareBitmapBaseline(tempOutputFile, TestUtil.GetTestFile(baselineImage));
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

        // Conversions started at the same time on different threads must both succeed; PDFium
        // is not thread-safe, so PdfConversion has to serialize them.
        [Test]
        public void ConcurrentConversions()
        {
            string fullPdfPath = TestUtil.GetTestFile("pdfconverter\\UWLetter.pdf");
            string[] outputFiles = { GetTempOutputFile(), GetTempOutputFile(), GetTempOutputFile() };

            try {
                Task[] tasks = new Task[outputFiles.Length];
                for (int i = 0; i < outputFiles.Length; ++i) {
                    string outputFile = outputFiles[i];
                    tasks[i] = Task.Run(() => PdfConversion.Convert(150, fullPdfPath, outputFile));
                }
                Task.WaitAll(tasks);

                long expectedLength = new FileInfo(outputFiles[0]).Length;
                foreach (string outputFile in outputFiles) {
                    Assert.IsTrue(File.Exists(outputFile));
                    Assert.AreEqual(expectedLength, new FileInfo(outputFile).Length);
                }
            }
            finally {
                foreach (string outputFile in outputFiles) {
                    if (File.Exists(outputFile))
                        File.Delete(outputFile);
                }
            }
        }

        [Test]
        public void MissingSourceFile()
        {
            string missingPdf = Path.Combine(TestUtil.GetTestFileDirectory(), "pdfconverter\\doesnotexist.pdf");
            Assert.Throws<FileNotFoundException>(() => PdfConversion.Convert(300, missingPdf, GetTempOutputFile()));
        }

        [Test]
        public void InvalidDpi()
        {
            string fullPdfPath = TestUtil.GetTestFile("pdfconverter\\UWLetter.pdf");
            Assert.Throws<ArgumentOutOfRangeException>(() => PdfConversion.Convert(0, fullPdfPath, GetTempOutputFile()));
        }

        // A file that is not a valid PDF must throw, not crash, and leave no output behind.
        [Test]
        public void CorruptPdf()
        {
            string corruptPdf = Path.Combine(TestUtil.GetTestFileDirectory(), $"pdfconverter\\corrupt_{GetRandomHex8()}_temp.pdf");
            string outputFile = GetTempOutputFile();
            File.WriteAllText(corruptPdf, "This is not a PDF file.");

            try {
                Assert.Throws<Exception>(() => PdfConversion.Convert(300, corruptPdf, outputFile));
                Assert.IsFalse(File.Exists(outputFile));
            }
            finally {
                File.Delete(corruptPdf);
                if (File.Exists(outputFile))
                    File.Delete(outputFile);
            }
        }
    }
}

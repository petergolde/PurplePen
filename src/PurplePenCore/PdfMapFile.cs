using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using PurplePen.PdfConverter;

namespace PurplePen
{
    // Manages a PDF map file and converting it to a bitmap.
    public class PdfMapFile
    {
        private string pdfFileName;
        private string pngFileName;
        private volatile ConversionStatus status;    // Set last by the background conversion, so the other fields are valid once it changes.
        private string conversionOutput;

        private const int Resolution = 600; // Resolution in DPI
        
        public PdfMapFile(string pdfFileName)
        {
            this.pdfFileName = pdfFileName;
            this.status = ConversionStatus.NotStarted;
        }

        public event EventHandler ConversionCompleted;

        public string PdfFileName {
            get { return pdfFileName; }
        }

        public string PngFileName
        {
            get
            {
                if (Status != ConversionStatus.Success)
                    throw new InvalidOperationException("Cannot get PNG file name until conversion is successful.");
                return pngFileName;
            }
        }

        public bool SourceExists {
            get {
                return File.Exists(pdfFileName);
            }
        }

        public ConversionStatus Status
        {
            get
            {
                return status;
            }
        }

        public string ConversionOutput
        {
            get {
                return conversionOutput;
            }
        }

        // Try to begin conversion into bitmap. 
        public ConversionStatus BeginConversion()
        {
            if (!SourceExists) {
                conversionOutput = string.Format("File '{0}' does not exist.", pdfFileName);
                status = ConversionStatus.Failure;
                return status;
            }

            CleanCacheDirectory();

            string cacheFileName = GetCacheFileName(pdfFileName);
            if (File.Exists(cacheFileName)) {
                // Cached file still exists. Use it.
                conversionOutput = "";
                pngFileName = cacheFileName;
                status = ConversionStatus.Success;
                return status;
            }

            return BeginUncachedConversion(cacheFileName, Resolution);
        }

        // Begin converting the PDF into a bitmap on a background thread. Returns Working; the
        // ConversionCompleted event is raised (on the background thread) when the conversion finishes.
        // Subscribe to ConversionCompleted before calling, since the conversion may finish at any time.
        //   fileName: the PNG file to create. May contain "%d" to convert every page.
        //   resolution: the resolution of the bitmap, in DPI.
        public ConversionStatus BeginUncachedConversion(string fileName, int resolution)
        {
            conversionOutput = "";
            pngFileName = fileName;
            status = ConversionStatus.Working;

            // RunConversion catches everything and reports through ConversionCompleted, so the task needn't be observed.
            _ = Task.Run(() => RunConversion(fileName, resolution));
            return ConversionStatus.Working;
        }

        // Do the conversion, then raise ConversionCompleted. Runs on a background thread.
        // Kept in its own method so the PdfConverter library is only loaded when a conversion actually happens.
        private void RunConversion(string fileName, int resolution)
        {
            ConversionStatus newStatus;

            try {
                PdfConversion.Convert(resolution, pdfFileName, fileName);
                newStatus = ConversionStatus.Success;
            }
            catch (Exception e) {
                conversionOutput = e.Message;
                newStatus = ConversionStatus.Failure;

                try {
                    if (!string.IsNullOrWhiteSpace(fileName))
                        File.Delete(fileName);
                }
                catch {
                    // Not a problem if a partial file can't be deleted.
                }
            }

            status = newStatus;

            if (ConversionCompleted != null)
                ConversionCompleted(this, EventArgs.Empty);
        }

        internal string GetCacheFileName(string path)
        {
            string cacheDirectory = GetCacheDirectory();

            return Path.Combine(cacheDirectory, CalculateSha1(path) + ".png");
        }

        private static string GetCacheDirectory()
        {
            string tempPath = Path.GetTempPath();
            string cacheDirectory = Path.Combine(tempPath, "PurplePen");
            if (!Directory.Exists(cacheDirectory))
                Directory.CreateDirectory(cacheDirectory);
            return cacheDirectory;
        }

        // Clean stale caches (over 6 months old).
        private static void CleanCacheDirectory()
        {
            DateTime oldDate = DateTime.Now.Subtract(TimeSpan.FromDays(180));
            string cacheDirectory = GetCacheDirectory();

            try {
                foreach (string filename in Directory.GetFiles(cacheDirectory, "*.png", SearchOption.TopDirectoryOnly)) {
                    FileInfo fileInfo = new FileInfo(filename);
                    if (fileInfo.Exists && fileInfo.LastWriteTime < oldDate) {
                        fileInfo.Delete();
                    }
                }
            }
            catch {
                // Do nothing. Not a problem if we get an exception here.
            }
        }

        internal string CalculateSha1(string path)
        {
            var hashAlgorithm = System.Security.Cryptography.SHA1.Create();
            byte[] hash = hashAlgorithm.ComputeHash(File.ReadAllBytes(path));
            hash[0] ^= 0xd9;   // Change hash so different from previous (GhostScript)
#if !NETFRAMEWORK
            // NET CORE use different hash from NET Framework, because the converters are different.
            hash[1] ^= 0x7b;   
#endif
            return Hexify(hash);
        }

        private string Hexify(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes) {
                builder.Append(b.ToString("X2"));
            }
            return builder.ToString();
        }

        public enum ConversionStatus
        {
            NotStarted, Success, Failure, Working
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;

namespace PurplePen
{
    // Manages a PDF map file and converting it to a bitmap.
    class PdfMapFile : IDisposable
    {
        private string pdfFileName;
        private string pngFileName;
        private ConversionStatus status;
        private string conversionOutput;
        private StringBuilder stderrOutput;
        private Process process;
        private bool disposed = false;

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
                Debug.Assert(Status == ConversionStatus.Success);
                return pngFileName;
            }
        }

        public bool SourceExists {
            get {
                return File.Exists(pdfFileName);
            }
        }

        public bool PdfConverterExists
        {
            get
            {
                return FindPdfConverterExe() != null;
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

        // Try to begin conversion into bitmap. 
        public ConversionStatus BeginUncachedConversion(string fileName, int resolution)
        {
            try {
                string converterExe = FindPdfConverterExe();
                if (converterExe == null) {
                    conversionOutput = MiscText.PdfConverterNotFound;
                    status = ConversionStatus.Failure;
                    return status;
                }

                string arguments = String.Format(
                    "{2} \"{0}\" \"{1}\"",
                    pdfFileName, fileName, resolution);

                stderrOutput = new StringBuilder();
                ProcessStartInfo startInfo = new ProcessStartInfo(converterExe, arguments);
                startInfo.CreateNoWindow = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                process = new Process();
                process.StartInfo = startInfo;
                process.EnableRaisingEvents = true;
                process.ErrorDataReceived += ProcessDataReceived;
                process.OutputDataReceived += ProcessDataReceived;
                process.Exited += ProcessExited;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                status = ConversionStatus.Working;
                pngFileName = fileName;
                return status;
            }
            catch (Exception e) {
                status = ConversionStatus.Failure;

                if (!string.IsNullOrWhiteSpace(pngFileName))
                    File.Delete(pngFileName);

                conversionOutput = e.Message;
                return status;
            }
        }

        private void ProcessDataReceived(object sender, DataReceivedEventArgs e)
        {
            lock (stderrOutput) {
                stderrOutput.Append(e.Data);
                stderrOutput.Append("\r\n");
            }
        }

        private void ProcessExited(object sender, EventArgs e)
        {
            process.WaitForExit();

            lock (stderrOutput) {
                conversionOutput = stderrOutput.ToString();
            }

            status = process.ExitCode == 0 ? ConversionStatus.Success : ConversionStatus.Failure;
            process.Dispose();
            process = null;

            if (status == ConversionStatus.Failure && !string.IsNullOrWhiteSpace(pngFileName))
                File.Delete(pngFileName);

            if (ConversionCompleted != null)
                ConversionCompleted(this, EventArgs.Empty);
        }

        internal string FindPdfConverterExe()
        {
            Uri uri = new Uri(typeof(PdfMapFile).Assembly.Location);
            string applicationDirectory = Path.GetDirectoryName(uri.LocalPath);
            return Path.Combine(applicationDirectory, "PdfConverter.exe");
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
            hash[0] ^= 0xe9;   // Change hash so different from previous (GhostScript)
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

        // Implement IDisposable to ensure the process field is disposed.
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources.
                try {
                    if (process != null)
                    {
                        process.Dispose();
                        process = null;
                    }
                }
                catch {
                    // Swallow exceptions during dispose.
                }
            }

            disposed = true;
        }

        public enum ConversionStatus
        {
            NotStarted, Success, Failure, Working
        }
    }
}

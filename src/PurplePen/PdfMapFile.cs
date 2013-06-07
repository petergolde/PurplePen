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
    class PdfMapFile
    {
        private string pdfFileName;
        private string pngFileName;
        private ConversionStatus status;
        private string conversionOutput;
        private const string Resolution = "600";  // Resolution to use for generating PNG file.
        private StringBuilder stderrOutput;
        private Process process;

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

        public bool GhostscriptInstalled
        {
            get
            {
                return FindGhostscriptExe() != null;
            }
        }

        public ConversionStatus Status
        {
            get
            {
                if (status == ConversionStatus.Working) {
                    CheckForCompletion();
                }

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
            try {
                if (!SourceExists) {
                    conversionOutput = string.Format("File '{0}' does not exist.", pdfFileName);
                    status = ConversionStatus.Failure;
                    return status;
                }

                string cacheFileName = GetCacheFileName(pdfFileName);
                if (File.Exists(cacheFileName)) {
                    // Cached file still exists. Use it.
                    conversionOutput = "";
                    pngFileName = cacheFileName;
                    status = ConversionStatus.Success;
                    return status;
                }

                string gsExe = FindGhostscriptExe();
                if (gsExe == null) {
                    conversionOutput = MiscText.GhostscriptNotInstalled;
                    status = ConversionStatus.Failure;
                }

                string arguments = String.Format(
                    "-q -dSAFER -dBATCH -dNOPAUSE -r{2} -dTextAlphaBits=4 -dGraphicsAlphaBits=4 -sDEVICE=png16m -sOutputFile=\"{1}\" \"{0}\"",
                    pdfFileName, cacheFileName, Resolution);

                stderrOutput = new StringBuilder();
                ProcessStartInfo startInfo = new ProcessStartInfo(gsExe, arguments);
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
                return status;
            }
            catch (Exception e) {
                status = ConversionStatus.Failure;
                conversionOutput = e.Message;
                return status;
            }
        }

        private void ProcessDataReceived(object sender, DataReceivedEventArgs e)
        {
            stderrOutput.Append(e.Data);
            stderrOutput.Append("\r\n");
        }

        private void ProcessExited(object sender, EventArgs e)
        {
            conversionOutput = stderrOutput.ToString();
            status = process.ExitCode == 0 ? ConversionStatus.Success : ConversionStatus.Failure;
            process.Dispose();
            process = null;

            if (ConversionCompleted != null)
                ConversionCompleted(this, EventArgs.Empty);
        }

        internal string FindGhostscriptExe()
        {
            // The GhostScript EXE is found by looking in the registry path:
            //   HKEY_LOCAL_MACHINE\Software\Artifex\GPL Ghostscript\<version number>
            // where we use the largest version number we can find.
            RegistryKey gsKey;
            string latestValue;
            try {
                gsKey = Registry.LocalMachine.OpenSubKey("Software\\Artifex\\GPL Ghostscript");
            }
            catch (Exception) {
                return null;
            }
            if (gsKey == null)
                return null;

            using (gsKey) {
                var subKeys = gsKey.GetSubKeyNames();
                if (subKeys == null || subKeys.Length == 0)
                    return null;

                try {
                    var latestKeyName = (from versionString in gsKey.GetSubKeyNames() orderby new Version(versionString) descending select versionString).First();
                    var latestKey = gsKey.OpenSubKey(latestKeyName);
                    latestValue = (string)(latestKey.GetValue(null));
                    latestKey.Dispose();
                }
                catch (Exception) {
                    return null;
                }

                string ghostScriptPath = latestValue + "\\bin\\gswin32c.exe";
                if (File.Exists(ghostScriptPath))
                    return ghostScriptPath;
                else
                    return null;
            }
        }

        internal string GetCacheFileName(string path)
        {
            string tempPath = Path.GetTempPath();
            string cacheDirectory = Path.Combine(tempPath, "PurplePen");
            if (!Directory.Exists(cacheDirectory))
                Directory.CreateDirectory(cacheDirectory);

            return Path.Combine(cacheDirectory, CalculateSha1(path) + ".png");
        }

        internal string CalculateSha1(string path)
        {
            var hashAlgorithm = System.Security.Cryptography.SHA1.Create();
            byte[] hash = hashAlgorithm.ComputeHash(File.ReadAllBytes(path));
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

        // Check to see if a conversion has completed.
        private void CheckForCompletion()
        {
            throw new NotImplementedException();
        }

        public enum ConversionStatus
        {
            NotStarted, Success, Failure, Working
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurplePen
{
    // The Windows Store version cannot automatically install the Roboto
    // fonts. When doing most operations, we just rely on the private fonts, so
    // its fine. But if you export to OCAD/OOM file, you need the Roboto fonts
    // installed on the system. So when the user does that, we check if the Roboto
    // fonts are installed, and install them if the user wants.
    internal static class FontInstallation
    {
        // After installing, GDI+ doesn't seem to notice the new fonts until the program is restarted.
        // So we remember if we've installed the fonts, and don't check again until the program is restarted.
        private static bool robotoFontsInstalled = false;

        // Check to see if the Roboto and Roboto Condensed fonts are installed
        public static bool AreRobotoFontsInstalledOnSystem()
        {
            if (robotoFontsInstalled)
                return true;  // already checked or already installed.

            // FontFamily constructor throws if the family is not available.
            try {
                bool allStyles = true;
                FontFamily roboto = new FontFamily("Roboto");
                if (roboto == null)
                    return false;

                allStyles &= roboto.IsStyleAvailable(FontStyle.Regular);
                allStyles &= roboto.IsStyleAvailable(FontStyle.Bold);
                roboto.Dispose();

                FontFamily condensed = new FontFamily("Roboto Condensed");
                if (condensed == null)
                    return false;
                
                allStyles &= condensed.IsStyleAvailable(FontStyle.Regular);
                allStyles &= condensed.IsStyleAvailable(FontStyle.Bold);
                condensed.Dispose();

                robotoFontsInstalled = allStyles;
                return allStyles;
            }
            catch {
                return false;
            }
        }


        // Run the installer that is in the program directory that installs
        // the Roboto fonts. Returns true if it seemed to run (can't really
        // tell if the user installed it).
        public static bool RunRobotoFontInstaller()
        {
            string pathname = Util.GetFileInAppDirectory("Roboto Font Installer.exe");
            if (!File.Exists(pathname)) {
                return false;
            }

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            process.StartInfo.FileName = pathname;
            process.StartInfo.Arguments = "";
            try {
                process.Start();
                process.WaitForExit();
                robotoFontsInstalled = true;
                return true;
            }
            catch (Exception) {
                return false;
            }
        }
    }
}

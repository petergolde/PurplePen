using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace CrashReporterDotNET
{
    public static class HelperMethods
    {
        private static bool IsOS64Bit()
        {
            return Environment.Is64BitOperatingSystem;
        }

        public static string GetWindowsVersion()
        {
            string osArchitecture;
            Version windowsVersion = Environment.OSVersion.Version;
            try
            {
                osArchitecture = IsOS64Bit() ? "64" : "32";
            }
            catch (Exception)
            {
                osArchitecture = "Undetermined";
            }
            switch (windowsVersion.Major)
            {
                case 5:
                    switch (windowsVersion.Minor)
                    {
                        case 0:
                            return string.Format("Windows 2000 {0} {1} Version {2}",
                                                 Environment.OSVersion.ServicePack, osArchitecture,
                                                 windowsVersion);
                        case 1:
                            return string.Format("Windows XP {0} {1} Version {2}",
                                                 Environment.OSVersion.ServicePack, osArchitecture,
                                                 windowsVersion);
                        case 2:
                            return string.Format(
                                "Windows XP x64 Professional Edition / Windows Server 2003 {0} {1} Version {2}",
                                Environment.OSVersion.ServicePack, osArchitecture, windowsVersion);
                    }
                    break;
                case 6:
                    switch (Environment.OSVersion.Version.Minor)
                    {
                        case 0:
                            return string.Format("Windows Vista {0} {1} bit Version {2}",
                                                 Environment.OSVersion.ServicePack, osArchitecture,
                                                 windowsVersion);
                        case 1:
                            return string.Format("Windows 7 {0} {1} bit Version {2}",
                                                 Environment.OSVersion.ServicePack, osArchitecture,
                                                 windowsVersion);
                        case 2:
                            return string.Format("Windows 8 {0} {1} bit Version {2}",
                                                 Environment.OSVersion.ServicePack, osArchitecture,
                                                 windowsVersion);
                        case 3:
                            return string.Format("Windows 8.1 {0} {1} bit Version {2}",
                                                 Environment.OSVersion.ServicePack, osArchitecture,
                                                 windowsVersion);
                    }
                    break;

                case 10:
                    switch (Environment.OSVersion.Version.Minor) {
                        case 0:
                            return string.Format("Windows 10 {0} {1} bit Version {2}",
                                                 Environment.OSVersion.ServicePack, osArchitecture,
                                                 windowsVersion);
                    }
                    break;
            }

            return string.Format("Unknown {0} bit Version {1}", osArchitecture,
                                         windowsVersion);
        }

        public static string GetLoadedAssemblies()
        {
            StringBuilder builder = new StringBuilder();

            foreach (Assembly assem in AppDomain.CurrentDomain.GetAssemblies()) {
                builder.AppendLine(GetAssemblyInfo(assem));
            }

            return builder.ToString();
        }

        private static string GetAssemblyInfo(Assembly assem)
        {
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assem.Location);
            string fileVersion = fvi.FileVersion;

            return string.Format("{0}, FileVersion={1}",
                assem.FullName,
                fileVersion);
        }

        public static string GetCultureInfo(CultureInfo culture)
        {
            return culture.Name + ": " + culture.EnglishName;
        }
    }
}

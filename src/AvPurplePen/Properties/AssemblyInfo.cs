using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Purple Pen")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Golde Software")]
[assembly: AssemblyProduct("Purple Pen")]
[assembly: AssemblyCopyright("Copyright © 2007-2026")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM componenets.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]


[assembly: AssemblyVersion(PurplePen.MapModel.VersionNumber.Current)]
[assembly: AssemblyFileVersion(PurplePen.MapModel.VersionNumber.Current)]

#if WINDOWS
// The Windows build (net10.0-windows) only runs on Windows. The SDK normally generates this
// attribute for -windows target frameworks, but not with GenerateAssemblyInfo turned off.
// Without it, every use of a Windows-only API (e.g. Windows Forms printing) raises CA1416.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows7.0")]
#endif

#if MACOS
// Likewise for the macOS build (net10.0-macos). Keep the version in step with
// SupportedOSPlatformVersion in AvPurplePen.csproj. Without it, every use of SkiaSharp (whose
// macOS assembly is annotated as macOS 12+) and of any AppKit API raises CA1416.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("macos13.0")]
#endif



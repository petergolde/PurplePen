using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("PdfConverter")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Golde Software")]
[assembly: AssemblyProduct("Purple Pen")]
[assembly: AssemblyCopyright("Copyright © 2007-2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("89318625-2f71-4c44-bfb1-8984f4d06152")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion(VersionNumber.Current)]
[assembly: AssemblyFileVersion(VersionNumber.Current)]

// Contains the current version number.
// NOTE: WHEN CHANGING THIS, YOU MUST ALSO CHANGE THE VERSION PROPERTY OF THE SETUP PROJECT 
// (Go to solution explorer, highligh setup project, choose View/Properties Window.) Also, change the 
// product code (you will be prompted for this -- say yes).
static class VersionNumber
{
    public const string Current = "3.0.0.211";

    // The last component encodes the Alpha/Beta/RC/Stable notion.
    // 101 is Alpha 1, 220 is Beta 2, 500 is stable release
    public const int Alpha = 100;
    public const int Beta = 200;
    public const int RC = 300;
    public const int Stable = 500;
}


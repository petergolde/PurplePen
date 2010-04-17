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
[assembly: AssemblyCopyright("Copyright © 2007")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM componenets.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f4eead18-8c4a-46b1-be28-9c6eb7222e8f")]

[assembly: AssemblyVersion(VersionNumber.Current)]
[assembly: AssemblyFileVersion(VersionNumber.Current)]

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("PurplePen_Tests")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//

static class VersionNumber
{
    public const string Current = "1.0.1.5000";

    // The last component encodes the Alpha/Beta/RC/Stable notion.
    // 1100 is Alpha 1, 2200 is Beta 2, 5000 is stable release
    public const int Alpha = 1000;
    public const int Beta = 2000;
    public const int RC = 3000;
    public const int Stable = 5000;
}


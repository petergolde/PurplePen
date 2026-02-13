using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Purple Pen")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Golde Software")]
[assembly: AssemblyProduct("Purple Pen")]
[assembly: AssemblyCopyright("Copyright © 2007-2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM componenets.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f4eead18-8c4a-46b1-be28-9c6eb7222e8f")]

[assembly: AssemblyVersion(PurplePen.VersionNumber.Current)]
[assembly: AssemblyFileVersion(PurplePen.VersionNumber.Current)]

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("PurplePen_Tests")]

#if NET5_0_OR_GREATER
[assembly: SupportedOSPlatform("windows")]
#endif


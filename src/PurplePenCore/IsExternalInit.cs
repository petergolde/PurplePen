// IsExternalInit.cs
//
// Polyfill for System.Runtime.CompilerServices.IsExternalInit, which the C# compiler
// requires when emitting init-only setters (used by records and init accessors).
// This type exists in .NET 5+ but not in .NET Framework 4.8, so define it here for
// the net48 target only.

#if NETFRAMEWORK
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
#endif

using System.Runtime.Versioning;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExecutionScope = Microsoft.VisualStudio.TestTools.UnitTesting.ExecutionScope;

[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
[assembly: SupportedOSPlatform("windows")]

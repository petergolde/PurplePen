using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.Versioning;
using ExecutionScope = Microsoft.VisualStudio.TestTools.UnitTesting.ExecutionScope;

[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
[assembly: SupportedOSPlatform("windows")]

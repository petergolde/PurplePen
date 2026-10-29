using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using ExecutionScope = Microsoft.VisualStudio.TestTools.UnitTesting.ExecutionScope;

[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
[assembly: Parallelizable(ParallelScope.Fixtures)]

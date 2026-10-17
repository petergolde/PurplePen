using System;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.InProcDataCollector;

namespace PurplePen.Testing
{
    // Writes a line the moment each test begins, so that a run in progress can
    // be followed and a run that has stopped moving can be told apart from one
    // that is merely slow.
    //
    // The console logger cannot do this: it subscribes to TestResult and
    // TestRunComplete, so a test is reported only once it is over, and the test
    // that is currently hanging is the one test it will never name. The test
    // platform raises TestCaseStart to data collectors alone, which is why this
    // is a collector rather than a logger.
    //
    // The destination comes from the TEST_PROGRESS_LOG environment variable.
    // Unset, the collector does nothing, so a developer's run is unaffected by
    // its presence in the run settings.
    public class TestStartLog : InProcDataCollection
    {
        private string fileName;
        private readonly object sync = new object();

        // Parameters:
        //   dataCollectionSink - the platform's channel for attachments, which
        //     this collector does not use; its output has to be readable while
        //     the run is still going, and attachments arrive at the end.
        public void Initialize(IDataCollectionSink dataCollectionSink)
        {
            fileName = Environment.GetEnvironmentVariable("TEST_PROGRESS_LOG");
        }

        public void TestSessionStart(TestSessionStartArgs testSessionStartArgs)
        {
        }

        // Appends the time and the name of the test about to run.
        //
        // Parameters:
        //   testCaseStartArgs - carries the test case; null on a malformed
        //     event, which is not worth bringing a run down over.
        public void TestCaseStart(TestCaseStartArgs testCaseStartArgs)
        {
            if (string.IsNullOrEmpty(fileName) || testCaseStartArgs == null || testCaseStartArgs.TestCase == null)
                return;

            string name = testCaseStartArgs.TestCase.FullyQualifiedName;
            if (string.IsNullOrEmpty(name))
                name = testCaseStartArgs.TestCase.DisplayName;

            string line = string.Format(CultureInfo.InvariantCulture, "{0:HH:mm:ss}  {1}",
                                        DateTime.Now, name);

            // A solution's test projects run in several hosts at once, each
            // appending to the same file. The lock orders this host's writes;
            // between hosts the file is opened and closed per line, which is
            // slow but keeps a line whole. Losing a line to contention matters
            // less than failing a run over its own progress log.
            try {
                lock (sync) {
                    using (StreamWriter writer = new StreamWriter(fileName, true)) {
                        writer.WriteLine(line);
                    }
                }
            }
            catch (IOException) {
            }
            catch (UnauthorizedAccessException) {
            }
        }

        public void TestCaseEnd(TestCaseEndArgs testCaseEndArgs)
        {
        }

        public void TestSessionEnd(TestSessionEndArgs testSessionEndArgs)
        {
        }
    }
}

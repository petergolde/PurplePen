#if TEST
using System.IO;
using System.Linq;
using TestingUtils;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PurplePen.Livelox;
using System.Threading.Tasks;

namespace PurplePen.Tests
{
    [TestClass]
    public class LiveloxTests : TestFixtureBase
    {
        private TestUI ui;
        private Controller controller;

        [TestInitialize]
        public void Setup()
        {
            ui = TestUI.Create();
            controller = ui.controller;
        }

        [TestMethod]
        public async Task CreateLiveloxImportableEvent()
        {
            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile(@"livelox\Test Event.ppen"), false);
            Assert.IsTrue(success);

            var manager = new PublishManager();

            var temporaryDirectory = manager.CreateTemporaryDirectory();

            var importableEvent = manager.CreateImportableEvent(controller, ui.symbolDB, 1, temporaryDirectory);

            try
            {
                Assert.AreEqual("Test Event", importableEvent.Name);
                Assert.AreEqual(1, importableEvent.CourseDataFileNames.Length);
                Assert.AreEqual(2, importableEvent.CourseImageFileNames.Length);
                Assert.AreEqual(1, importableEvent.Maps.Length);

                foreach (var fileName in importableEvent.CourseDataFileNames.Concat(importableEvent.CourseImageFileNames).Concat(importableEvent.Maps.Select(o => o.FileName)))
                {
                    Assert.IsTrue(File.Exists(Path.Combine(temporaryDirectory, fileName)));
                }
            }
            finally
            {
                manager.DeleteTemporatyDirectory(temporaryDirectory);
            }
        }
    }
}

#endif

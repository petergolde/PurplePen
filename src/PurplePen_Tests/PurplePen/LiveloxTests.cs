/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

#if TEST
using System.IO;
using System.Linq;
using TestingUtils;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PurplePen.Livelox;

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
        public void CreateLiveloxImportableEvent()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile(@"livelox\Test Event.ppen"), false);
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

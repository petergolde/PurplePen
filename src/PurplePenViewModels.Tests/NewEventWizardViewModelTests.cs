// NewEventWizardViewModelTests.cs
//
// Tests for NewEventWizardViewModel: page navigation (including the
// conditional bitmap-scale skip), per-page CanProceed validation, and the
// Finish path (CreateEventInfo assembly and the file pre-flight check).

using System;
using System.IO;
using NUnit.Framework;
using PurplePen;
using PurplePen.ViewModels;

namespace PurplePenViewModels.Tests
{
    /// <summary>
    /// Tests for the New Event wizard ViewModel.
    /// </summary>
    [TestFixture]
    public class NewEventWizardViewModelTests
    {
        private NewEventWizardViewModel vm = null!;

        [SetUp]
        public void Initialize()
        {
            vm = new NewEventWizardViewModel();
        }

        // ===== Navigation =====

        /// <summary>The wizard starts on the Title page.</summary>
        [Test]
        public void StartsOnTitlePage()
        {
            Assert.That(vm.CurrentPage, Is.EqualTo(WizardPage.Title));
            Assert.That(vm.IsTitlePage, Is.True);
            Assert.That(vm.CanGoBack, Is.False);
        }

        /// <summary>For an OCAD map, Next from the map-file page skips the bitmap-scale page.</summary>
        [Test]
        public void Next_SkipsBitmapScalePage_ForOcadMap()
        {
            vm.MapType = MapType.OCAD;
            vm.ScaleText = "15000";

            vm.NextCommand.Execute(null);   // Title -> MapFile
            Assert.That(vm.CurrentPage, Is.EqualTo(WizardPage.MapFile));

            vm.NextCommand.Execute(null);   // MapFile -> (skip BitmapScale) -> PrintScale
            Assert.That(vm.CurrentPage, Is.EqualTo(WizardPage.PrintScale));
        }

        /// <summary>For a bitmap map, Next from the map-file page shows the bitmap-scale page.</summary>
        [Test]
        public void Next_ShowsBitmapScalePage_ForBitmapMap()
        {
            vm.MapType = MapType.Bitmap;

            vm.NextCommand.Execute(null);   // Title -> MapFile
            vm.NextCommand.Execute(null);   // MapFile -> BitmapScale

            Assert.That(vm.CurrentPage, Is.EqualTo(WizardPage.BitmapScale));
        }

        /// <summary>For a PDF map, Next from the map-file page shows the bitmap-scale page.</summary>
        [Test]
        public void Next_ShowsBitmapScalePage_ForPdfMap()
        {
            vm.MapType = MapType.PDF;

            vm.NextCommand.Execute(null);   // Title -> MapFile
            vm.NextCommand.Execute(null);   // MapFile -> BitmapScale

            Assert.That(vm.CurrentPage, Is.EqualTo(WizardPage.BitmapScale));
        }

        /// <summary>Back from the print-scale page skips the bitmap-scale page for an OCAD map.</summary>
        [Test]
        public void Back_SkipsBitmapScalePage_ForOcadMap()
        {
            vm.MapType = MapType.OCAD;
            vm.ScaleText = "15000";
            vm.CurrentPage = WizardPage.PrintScale;

            vm.BackCommand.Execute(null);   // PrintScale -> (skip BitmapScale) -> MapFile

            Assert.That(vm.CurrentPage, Is.EqualTo(WizardPage.MapFile));
        }

        // ===== Per-page CanProceed =====

        /// <summary>Title page requires a non-empty title.</summary>
        [Test]
        public void CanProceed_TitlePage_RequiresTitle()
        {
            Assert.That(vm.CurrentPage, Is.EqualTo(WizardPage.Title));
            Assert.That(vm.CanProceed, Is.False);

            vm.TitleText = "My Event";
            Assert.That(vm.CanProceed, Is.True);
        }

        /// <summary>Directory page requires a folder only when "other folder" is chosen.</summary>
        [Test]
        public void CanProceed_DirectoryPage_RequiresFolderForOtherOnly()
        {
            vm.CurrentPage = WizardPage.Directory;

            // Default: use map directory -> always OK.
            Assert.That(vm.CanProceed, Is.True);

            // Other folder, empty path -> not OK.
            vm.UseOtherFolder = true;
            Assert.That(vm.CanProceed, Is.False);

            // Other folder, with a path -> OK.
            vm.DirectoryName = @"C:\Temp";
            Assert.That(vm.CanProceed, Is.True);
        }

        /// <summary>Bitmap-scale page requires a valid DPI and scale for a bitmap.</summary>
        [Test]
        public void CanProceed_BitmapScalePage_RequiresDpiAndScale()
        {
            vm.MapType = MapType.Bitmap;
            vm.CurrentPage = WizardPage.BitmapScale;

            vm.DpiText = "";
            vm.ScaleText = "15000";
            Assert.That(vm.CanProceed, Is.False);

            vm.DpiText = "300";
            Assert.That(vm.CanProceed, Is.True);
        }

        /// <summary>Bitmap-scale page requires only a scale (no DPI) for a PDF.</summary>
        [Test]
        public void CanProceed_BitmapScalePage_PdfRequiresOnlyScale()
        {
            vm.MapType = MapType.PDF;
            vm.CurrentPage = WizardPage.BitmapScale;

            vm.DpiText = "";
            vm.ScaleText = "15000";
            Assert.That(vm.CanProceed, Is.True);
        }

        /// <summary>Standards page is satisfied by the defaults seeded in the constructor.</summary>
        [Test]
        public void CanProceed_StandardsPage_DefaultsAreValid()
        {
            vm.CurrentPage = WizardPage.Standards;
            Assert.That(vm.CanProceed, Is.True);
        }

        // ===== Finish =====

        /// <summary>
        /// A successful Finish assembles CreateEventInfo from all the page values,
        /// creates the event file, and requests the dialog to close with true.
        /// </summary>
        [Test]
        public void Finish_AssemblesCreateEventInfoAndCreatesFile()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "PPenWizardTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try {
                vm.TitleText = "Sample Event";
                vm.MapType = MapType.OCAD;
                vm.MapFileName = Path.Combine(tempDir, "map.ocd");
                vm.ScaleText = "15000";
                vm.PrintScaleText = "10000";
                vm.PaperWidth = 850;
                vm.PaperHeight = 1100;
                vm.PaperMargin = 50;
                vm.PaperLandscape = false;
                vm.IsMap2000 = false;
                vm.IsMapSpr2019 = false;
                vm.IsMap2017 = true;
                vm.IsDescriptions2004 = false;
                vm.IsDescriptions2018 = true;
                vm.FirstCode = 41;
                vm.DisallowInvertibleCodes = true;
                vm.UseMapDirectory = false;
                vm.UseOtherFolder = true;
                vm.DirectoryName = tempDir;

                bool? closeResult = null;
                vm.RequestClose += result => closeResult = result;

                vm.CurrentPage = WizardPage.Final;
                vm.NextCommand.Execute(null);   // Finish

                Assert.That(closeResult, Is.True, "Finish should request close with true");
                Assert.That(vm.HasFinalError, Is.False);

                Controller.CreateEventInfo info = vm.CreateEventInfo;
                Assert.That(info.title, Is.EqualTo("Sample Event"));
                Assert.That(info.mapType, Is.EqualTo(MapType.OCAD));
                Assert.That(info.scale, Is.EqualTo(15000f));
                Assert.That(info.allControlsPrintScale, Is.EqualTo(10000f));
                Assert.That(info.firstCode, Is.EqualTo(41));
                Assert.That(info.disallowInvertibleCodes, Is.True);
                Assert.That(info.mapStandard, Is.EqualTo("2017"));
                Assert.That(info.descriptionStandard, Is.EqualTo("2018"));
                Assert.That(info.blend, Is.EqualTo(PurpleColorBlend.Blend));
                Assert.That(info.printArea.pageWidth, Is.EqualTo(850));
                Assert.That(info.printArea.pageHeight, Is.EqualTo(1100));
                Assert.That(info.printArea.pageMargins, Is.EqualTo(50));
                Assert.That(info.printArea.pageLandscape, Is.False);
                Assert.That(File.Exists(info.eventFileName), Is.True);
            }
            finally {
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// If the target file already exists, Finish shows an error on the final
        /// page and does not request the dialog to close.
        /// </summary>
        [Test]
        public void Finish_ShowsError_WhenFileAlreadyExists()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "PPenWizardTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try {
                vm.TitleText = "Existing Event";
                vm.MapType = MapType.OCAD;
                vm.ScaleText = "15000";
                vm.PrintScaleText = "15000";
                vm.UseMapDirectory = false;
                vm.UseOtherFolder = true;
                vm.DirectoryName = tempDir;

                // Pre-create the file the wizard would write.
                File.WriteAllText(Path.Combine(tempDir, "Existing Event.ppen"), "x");

                bool closeRequested = false;
                vm.RequestClose += _ => closeRequested = true;

                vm.CurrentPage = WizardPage.Final;
                vm.NextCommand.Execute(null);   // Finish

                Assert.That(closeRequested, Is.False, "Finish should not close when the file exists");
                Assert.That(vm.HasFinalError, Is.True);
                Assert.That(vm.FinalErrorMessage, Is.Not.Empty);
                Assert.That(vm.CanProceed, Is.False, "CanProceed is false while a final error is shown");
            }
            finally {
                Directory.Delete(tempDir, true);
            }
        }
    }
}

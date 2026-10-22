using System.Collections.Generic;
using System.ComponentModel;
using NUnit.Framework;
using PurplePen;
using PurplePen.MapModel;
using PurplePen.ViewModels;

namespace PurplePenViewModels.Tests
{
    /// <summary>
    /// Tests for PrintPreviewDialogViewModel: page list, current page, and
    /// page navigation commands.
    /// </summary>
    [TestFixture]
    public class PrintPreviewDialogViewModelTests
    {
        private PrintPreviewDialogViewModel vm = null!;
        private List<RecordingPrintTarget> printTargets = new List<RecordingPrintTarget>();

        [SetUp]
        public void Initialize()
        {
            vm = new PrintPreviewDialogViewModel();
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (RecordingPrintTarget printTarget in printTargets)
                printTarget.Dispose();
            printTargets.Clear();
        }

        // Create a RecordingPrintTarget that has recorded the given number of (blank) letter-size pages.
        private RecordingPrintTarget CreatePrintTarget(int pageCount)
        {
            RecordingPrintTarget printTarget = new RecordingPrintTarget(new SkiaBitmapGraphicsLoader());
            printTargets.Add(printTarget);

            PrintingPaperSize paperSize = new PrintingPaperSize("Letter", 850, 1100);
            printTarget.StartPrinting("Test Document", pageCount);
            for (int pageNumber = 1; pageNumber <= pageCount; ++pageNumber) {
                printTarget.PrintPage(pageNumber, paperSize, grTarget => { });
            }
            printTarget.EndPrinting();

            return printTarget;
        }

        [Test]
        public void NoPrintTarget()
        {
            Assert.That(vm.PageCount, Is.EqualTo(0));
            Assert.That(vm.HasPages, Is.False);
            Assert.That(vm.CurrentPageIndex, Is.EqualTo(-1));
            Assert.That(vm.CurrentPage, Is.Null);
            Assert.That(vm.CurrentPageNumber, Is.EqualTo(0));
            Assert.That(vm.DocumentTitle, Is.EqualTo(""));
            Assert.That(vm.FirstPageCommand.CanExecute(null), Is.False);
            Assert.That(vm.NextPageCommand.CanExecute(null), Is.False);
        }

        [Test]
        public void EmptyPrintTarget()
        {
            vm.PrintTarget = CreatePrintTarget(0);

            Assert.That(vm.HasPages, Is.False);
            Assert.That(vm.CurrentPageIndex, Is.EqualTo(-1));
            Assert.That(vm.CurrentPage, Is.Null);
            Assert.That(vm.NextPageCommand.CanExecute(null), Is.False);
            Assert.That(vm.LastPageCommand.CanExecute(null), Is.False);
        }

        [Test]
        public void SettingPrintTargetShowsFirstPage()
        {
            RecordingPrintTarget printTarget = CreatePrintTarget(3);
            vm.PrintTarget = printTarget;

            Assert.That(vm.PageCount, Is.EqualTo(3));
            Assert.That(vm.HasPages, Is.True);
            Assert.That(vm.DocumentTitle, Is.EqualTo("Test Document"));
            Assert.That(vm.CurrentPageIndex, Is.EqualTo(0));
            Assert.That(vm.CurrentPageNumber, Is.EqualTo(1));
            Assert.That(vm.CurrentPage, Is.SameAs(printTarget.Pages[0]));
            Assert.That(vm.CurrentPage!.PageNumber, Is.EqualTo(1));
        }

        [Test]
        public void NavigationCommands()
        {
            vm.PrintTarget = CreatePrintTarget(3);

            // On the first page: can only go forward.
            Assert.That(vm.FirstPageCommand.CanExecute(null), Is.False);
            Assert.That(vm.PreviousPageCommand.CanExecute(null), Is.False);
            Assert.That(vm.NextPageCommand.CanExecute(null), Is.True);
            Assert.That(vm.LastPageCommand.CanExecute(null), Is.True);

            vm.NextPageCommand.Execute(null);
            Assert.That(vm.CurrentPageIndex, Is.EqualTo(1));
            Assert.That(vm.PreviousPageCommand.CanExecute(null), Is.True);
            Assert.That(vm.NextPageCommand.CanExecute(null), Is.True);

            vm.LastPageCommand.Execute(null);
            Assert.That(vm.CurrentPageIndex, Is.EqualTo(2));
            Assert.That(vm.NextPageCommand.CanExecute(null), Is.False);
            Assert.That(vm.LastPageCommand.CanExecute(null), Is.False);

            vm.PreviousPageCommand.Execute(null);
            Assert.That(vm.CurrentPageIndex, Is.EqualTo(1));

            vm.FirstPageCommand.Execute(null);
            Assert.That(vm.CurrentPageIndex, Is.EqualTo(0));
        }

        [Test]
        public void ChangingPageRaisesPropertyChanged()
        {
            vm.PrintTarget = CreatePrintTarget(2);

            List<string?> changed = new List<string?>();
            vm.PropertyChanged += (object? sender, PropertyChangedEventArgs e) => changed.Add(e.PropertyName);

            vm.NextPageCommand.Execute(null);

            Assert.That(changed, Does.Contain(nameof(PrintPreviewDialogViewModel.CurrentPageIndex)));
            Assert.That(changed, Does.Contain(nameof(PrintPreviewDialogViewModel.CurrentPage)));
            Assert.That(changed, Does.Contain(nameof(PrintPreviewDialogViewModel.CurrentPageNumber)));
        }

        [Test]
        public void CurrentPageNumberIsClamped()
        {
            vm.PrintTarget = CreatePrintTarget(3);

            vm.CurrentPageNumber = 2;
            Assert.That(vm.CurrentPageIndex, Is.EqualTo(1));

            vm.CurrentPageNumber = 99;
            Assert.That(vm.CurrentPageIndex, Is.EqualTo(2));

            vm.CurrentPageNumber = 0;
            Assert.That(vm.CurrentPageIndex, Is.EqualTo(0));
        }

        [Test]
        public void ChangingPrintTargetResetsToFirstPage()
        {
            vm.PrintTarget = CreatePrintTarget(3);
            vm.LastPageCommand.Execute(null);

            vm.PrintTarget = CreatePrintTarget(2);
            Assert.That(vm.CurrentPageIndex, Is.EqualTo(0));
            Assert.That(vm.PageCount, Is.EqualTo(2));
        }
    }
}

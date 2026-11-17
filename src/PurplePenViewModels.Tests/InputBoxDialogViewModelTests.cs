using NUnit.Framework;
using PurplePen.ViewModels;

namespace PurplePenViewModels.Tests
{
    /// <summary>
    /// Tests for InputBoxDialogViewModel: OK enabling with and without AllowEmpty, and
    /// the IsOkEnabled change notifications.
    /// </summary>
    [TestFixture]
    public class InputBoxDialogViewModelTests
    {
        /// <summary>By default, empty text is accepted, as with Visual Basic's InputBox.</summary>
        [Test]
        public void EmptyAllowedByDefault()
        {
            InputBoxDialogViewModel vm = new InputBoxDialogViewModel();

            Assert.That(vm.AllowEmpty, Is.True);
            Assert.That(vm.Text, Is.EqualTo(""));
            Assert.That(vm.IsOkEnabled, Is.True);
        }

        /// <summary>With AllowEmpty false, OK needs non-whitespace text, and notifications follow the text.</summary>
        [Test]
        public void OkNeedsTextWhenEmptyNotAllowed()
        {
            InputBoxDialogViewModel vm = new InputBoxDialogViewModel { AllowEmpty = false };
            int notifications = 0;
            vm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(vm.IsOkEnabled)) notifications++; };

            Assert.That(vm.IsOkEnabled, Is.False);
            vm.Text = "   ";
            Assert.That(vm.IsOkEnabled, Is.False);
            vm.Text = "fr";
            Assert.That(vm.IsOkEnabled, Is.True);
            Assert.That(notifications, Is.EqualTo(2));
        }
    }
}

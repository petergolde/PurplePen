using System.Globalization;
using System.Linq;
using NUnit.Framework;
using PurplePen;
using PurplePen.ViewModels;

namespace PurplePenViewModels.Tests
{
    /// <summary>
    /// Tests for AddDescriptionLanguageDialogViewModel: the culture list, filling in the
    /// native name, the plural noun/modifier interaction, OK enabling, and building the
    /// resulting SymbolLanguage.
    /// </summary>
    [TestFixture]
    public class AddDescriptionLanguageDialogViewModelTests
    {
        /// <summary>The culture list is sorted, non-empty, and leaves out the invariant culture.</summary>
        [Test]
        public void AllCulturesSortedWithoutInvariant()
        {
            AddDescriptionLanguageDialogViewModel vm = new AddDescriptionLanguageDialogViewModel();

            Assert.That(vm.AllCultures, Is.Not.Empty);
            Assert.That(vm.AllCultures.Any(c => c.Name == ""), Is.False);
            Assert.That(vm.AllCultures.Select(c => c.Name), Is.Ordered.Using<string>(System.StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>Choosing a culture fills in its native name.</summary>
        [Test]
        public void SelectingCultureSetsLanguageName()
        {
            AddDescriptionLanguageDialogViewModel vm = new AddDescriptionLanguageDialogViewModel();
            CultureInfo french = vm.AllCultures.First(c => c.Name == "fr");

            vm.SelectedCulture = french;

            Assert.That(vm.LanguageName, Is.EqualTo(french.NativeName));
        }

        /// <summary>Changing plural nouns clears plural modifiers, as in the WinForms dialog.</summary>
        [Test]
        public void ChangingPluralNounsClearsPluralModifiers()
        {
            AddDescriptionLanguageDialogViewModel vm = new AddDescriptionLanguageDialogViewModel();
            vm.PluralNouns = true;
            vm.PluralModifiers = true;

            vm.PluralNouns = false;

            Assert.That(vm.PluralModifiers, Is.False);
        }

        /// <summary>OK needs both a new culture and a language to copy from, and notifies when that changes.</summary>
        [Test]
        public void IsOkEnabledRequiresCultureAndCopyFrom()
        {
            AddDescriptionLanguageDialogViewModel vm = new AddDescriptionLanguageDialogViewModel();
            int notifications = 0;
            vm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(vm.IsOkEnabled)) notifications++; };

            Assert.That(vm.IsOkEnabled, Is.False);
            vm.SelectedCulture = vm.AllCultures.First(c => c.Name == "de");
            Assert.That(vm.IsOkEnabled, Is.False);
            vm.CopyFromLangId = "en";
            Assert.That(vm.IsOkEnabled, Is.True);
            Assert.That(notifications, Is.EqualTo(2));
        }

        /// <summary>The SymbolLanguage splits genders and cases, and ignores them when their checkbox is off.</summary>
        [Test]
        public void SymbolLanguageFromFields()
        {
            AddDescriptionLanguageDialogViewModel vm = new AddDescriptionLanguageDialogViewModel();
            vm.SelectedCulture = vm.AllCultures.First(c => c.Name == "de");
            vm.LanguageName = "Deutsch";
            vm.PluralNouns = true;
            vm.PluralModifiers = true;
            vm.GenderModifiers = true;
            vm.Genders = "masculine, feminine,neuter";
            vm.CaseModifiers = false;
            vm.Cases = "nominative dative";

            SymbolLanguage lang = vm.SymbolLanguage;

            Assert.That(lang.Name, Is.EqualTo("Deutsch"));
            Assert.That(lang.LangId, Is.EqualTo("de"));
            Assert.That(lang.PluralNouns, Is.True);
            Assert.That(lang.PluralModifiers, Is.True);
            Assert.That(lang.GenderModifiers, Is.True);
            Assert.That(lang.Genders, Is.EqualTo(new[] { "masculine", "feminine", "neuter" }));
            Assert.That(lang.CaseModifiers, Is.False);
            Assert.That(lang.Cases, Is.Empty);
        }
    }
}

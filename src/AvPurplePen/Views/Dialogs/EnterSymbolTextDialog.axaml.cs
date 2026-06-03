// EnterSymbolTextDialog.axaml.cs
//
// Code-behind for the "Customized Symbol Text" dialog. All of the logic lives
// in EnterSymbolTextDialogViewModel; this file does only the few things that
// can't be data-bound:
//   * keeps the grid's Number / Gender / Case columns shown or hidden in step
//     with the form check boxes (DataGrid columns aren't in the visual tree,
//     so their IsVisible can't be bound), and
//   * validates the fill-in "*" requirement on OK, keeping the dialog open
//     with an error message if a text is missing its "*".
//
// Migrated from WinForms PurplePen/EnterSymbolText.cs.

using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for entering the customized text for a symbol in a single
    /// language across its grammatical forms. The caller must set DataContext
    /// to a fully configured <see cref="EnterSymbolTextDialogViewModel"/> before
    /// showing, and reads back its SymbolTexts after OK.
    /// </summary>
    public partial class EnterSymbolTextDialog : Window
    {
        // Column indices in the DataGrid, matching the order declared in XAML.
        private const int NumberColumnIndex = 0;
        private const int GenderColumnIndex = 1;
        private const int CaseColumnIndex = 2;

        public EnterSymbolTextDialog()
        {
            InitializeComponent();
            Opened += (s, e) => dataGridView.Focus();
            DataContextChanged += OnDataContextChanged;
        }

        // When the ViewModel is attached, sync the column visibility once and
        // subscribe so it stays in step as the user toggles the check boxes.
        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (DataContext is EnterSymbolTextDialogViewModel vm) {
                vm.PropertyChanged += OnViewModelPropertyChanged;
                UpdateColumnVisibility(vm);
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (DataContext is EnterSymbolTextDialogViewModel vm &&
                (e.PropertyName == nameof(EnterSymbolTextDialogViewModel.NumberColumnVisible) ||
                 e.PropertyName == nameof(EnterSymbolTextDialogViewModel.GenderColumnVisible) ||
                 e.PropertyName == nameof(EnterSymbolTextDialogViewModel.CaseColumnVisible))) {
                UpdateColumnVisibility(vm);
            }
        }

        // Show / hide the Number, Gender and Case columns to match the VM.
        private void UpdateColumnVisibility(EnterSymbolTextDialogViewModel vm)
        {
            dataGridView.Columns[NumberColumnIndex].IsVisible = vm.NumberColumnVisible;
            dataGridView.Columns[GenderColumnIndex].IsVisible = vm.GenderColumnVisible;
            dataGridView.Columns[CaseColumnIndex].IsVisible = vm.CaseColumnVisible;
        }

        /// <summary>
        /// Validates the fill-in requirement and closes with OK if every text is
        /// valid. When translating fill-in placeholders, each text must contain
        /// a "*" — a missing one keeps the dialog open with an error message and
        /// selects the offending row.
        /// </summary>
        private async void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not EnterSymbolTextDialogViewModel vm) {
                Close(false);
                return;
            }

            SymbolTextRow? missingStar = vm.FindRowMissingStar();
            if (missingStar != null) {
                dataGridView.SelectedItem = missingStar;
                await ShowErrorAsync(MiscText.RequireStar);
                return;
            }

            Close(true);
        }

        /// <summary>Cancels and closes the dialog.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        // Shows a modal error message box parented to this dialog (the
        // DialogService walks the owner chain, so it's modal relative to us).
        private static async Task ShowErrorAsync(string message)
        {
            MessageBoxDialogViewModel mb = new MessageBoxDialogViewModel {
                Message = message,
                Buttons = MessageBoxButtons.Ok,
                DefaultButton = MessageBoxButton.Ok,
                Icon = MessageBoxIcon.Error,
            };
            await Services.DialogService.ShowDialogAsync(mb);
        }
    }
}

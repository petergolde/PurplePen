// CourseLoadDialog.axaml.cs
//
// Code-behind for the Course Load dialog. The grid is data-bound to
// CourseLoadDialogViewModel.Rows; this file handles the OK button's
// validation: each competitor-load cell must be blank or a valid integer.
// Invalid input shows an error message box (parented to this dialog) and
// keeps the dialog open, selecting the offending row.
//
// Migrated from WinForms PurplePen/CourseLoad.cs.

using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for setting the competitor load of every course. The caller must
    /// set DataContext to a <see cref="CourseLoadDialogViewModel"/> (with
    /// CourseLoads populated) before showing.
    /// </summary>
    public partial class CourseLoadDialog : Window
    {
        public CourseLoadDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Validates the loads and closes with OK if they're all valid.
        /// Mirrors the WinForms cell-validation: a load must be blank or an
        /// integer.
        /// </summary>
        private async void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CourseLoadDialogViewModel vm) {
                Close(false);
                return;
            }

            LoadRow? invalid = vm.FindInvalidLoad();
            if (invalid != null) {
                grid.SelectedItem = invalid;
                await ShowErrorAsync(MiscText.BadLoad);
                return;
            }

            Close(true);
        }

        /// <summary>Cancels and closes the dialog.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        /// <summary>
        /// Shows a modal error message box parented to this dialog (the
        /// DialogService walks the owner chain, so it's modal relative to us).
        /// </summary>
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

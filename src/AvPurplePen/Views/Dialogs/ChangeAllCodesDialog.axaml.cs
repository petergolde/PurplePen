// ChangeAllCodesDialog.axaml.cs
//
// Code-behind for the Change Control Codes dialog. The grid is data-bound to
// ChangeAllCodesDialogViewModel.Rows; this file handles the OK button's
// validation: every new code must be a legal control code, and no two
// controls may share a code. Invalid input shows an error message box
// (parented to this dialog) and keeps the dialog open. The duplicate / illegal
// row is selected to point the user at the problem.
//
// Migrated from WinForms PurplePen/ChangeAllCodes.cs.

using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for changing every control's code at once. The caller must set
    /// DataContext to a <see cref="ChangeAllCodesDialogViewModel"/> (with
    /// EventDB and Codes populated) before showing.
    /// </summary>
    public partial class ChangeAllCodesDialog : Window
    {
        public ChangeAllCodesDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Validates the codes and closes with OK if they're all valid.
        /// Mirrors the WinForms OkButtonClicked / cell-validation logic:
        /// illegal codes and duplicates are rejected with an error message.
        /// </summary>
        private async void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not ChangeAllCodesDialogViewModel vm) {
                Close(false);
                return;
            }

            // Every new code must be a legal control code.
            CodeRow? illegal = vm.FindIllegalCode(out string? reason);
            if (illegal != null) {
                grid.SelectedItem = illegal;
                await ShowErrorAsync(reason ?? "");
                return;
            }

            // No two controls may share a code.
            CodeRow? duplicate = vm.FindDuplicateCode();
            if (duplicate != null) {
                grid.SelectedItem = duplicate;
                await ShowErrorAsync(string.Format(MiscText.DuplicateCode, duplicate.NewCode));
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

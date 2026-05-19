// OperationInProgressDialog.axaml.cs
//
// Code-behind for the "Operation In Progress" dialog. All state is bound to
// the OperationInProgressDialogViewModel; this file just handles the Cancel
// button. The dialog is typically shown by the caller via
// IDialogService.ShowOwnedDialog(vm, disableOwner: true) — the caller
// dismisses it programmatically via the returned ICloseableDialog when the
// background operation finishes.
//
// Migrated from WinForms PurplePen/OperationInProgress.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Modal dialog showing a status label and a progress bar while a
    /// long-running operation runs. The caller must set DataContext to an
    /// <see cref="PurplePen.ViewModels.OperationInProgressDialogViewModel"/>
    /// before showing.
    /// </summary>
    public partial class OperationInProgressDialog : Window
    {
        public OperationInProgressDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Cancel button closes the dialog with false; the awaiting caller
        /// sees the false result and should abort the operation.
        /// </summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
